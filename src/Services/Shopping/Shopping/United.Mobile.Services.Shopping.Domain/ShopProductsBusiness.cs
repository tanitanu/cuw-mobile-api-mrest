using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.PersonalizationModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FareColumnEntitlements;
using United.Utility.Helper;
using static United.Mobile.Model.Shopping.Option;
using United.Utility.Enum;


namespace United.Mobile.Services.Shopping.Domain
{
    public class ShopProductsBusiness : IShopProductsBusiness
    {
        private readonly ICacheLog<ShopProductsBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IShoppingCcePromoService _shoppingCcePromoService;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IDPService _dPService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IFeatureSettings _featureSettings;

        public ShopProductsBusiness(ICacheLog<ShopProductsBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IShoppingCcePromoService shoppingCcePromoService
            , IFlightShoppingService flightShoppingService
            , IDPService dPService
            , IDynamoDBService dynamoDBService,
            ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IFFCShoppingcs fFCShoppingcs
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _shoppingCcePromoService = shoppingCcePromoService;
            _flightShoppingService = flightShoppingService;
            _dPService = dPService;
            _dynamoDBService = dynamoDBService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _fFCShoppingcs = fFCShoppingcs;
            _featureSettings = featureSettings;
        }
        public async Task<ChasePromoRedirectResponse> ChasePromoRTIRedirect(ChasePromoRedirectRequest chasePromoRedirectRequest)
        {
            ChasePromoRedirectResponse response = new ChasePromoRedirectResponse();
            bool validWalletRequest = false;

            validWalletRequest = await _shoppingUtility.ValidateHashPinAndGetAuthToken(chasePromoRedirectRequest.MileagePlusNumber, chasePromoRedirectRequest.HashPinCode, chasePromoRedirectRequest.Application.Id, chasePromoRedirectRequest.DeviceId, chasePromoRedirectRequest.Application.Version.Major);

            if (!validWalletRequest)
                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));

            if (validWalletRequest)
            {
                response.LanguageCode = chasePromoRedirectRequest.LanguageCode;
                response.TransactionId = chasePromoRedirectRequest.TransactionId;

                if (_configuration.GetValue<bool>("EnableChaseBannerFromCCE") && GeneralHelper.IsApplicationVersionGreater(chasePromoRedirectRequest.Application.Id, chasePromoRedirectRequest.Application.Version.Major, "AndroidChaseCCEPromoVersion", "iPhoneChaseCCEPromoVersion", "", "", true, _configuration))
                {
                    try
                    {
                        CCEPromo ccePromoPersist = new CCEPromo();
                        var ccePromoFromSession = await _sessionHelperService.GetSession<CCEPromo>(chasePromoRedirectRequest.SessionId, ccePromoPersist.ObjectName, new List<string> { chasePromoRedirectRequest.SessionId, ccePromoPersist.ObjectName });
                        var cceResponseFromSession = JsonConvert.DeserializeObject<ContextualCommResponse>(ccePromoFromSession.ContextualCommResponseJson);

                        if (cceResponseFromSession != null && cceResponseFromSession.Components.Count > 0
                            && cceResponseFromSession.Components[0].ContextualElements.Count > 0
                            && cceResponseFromSession.Components[0].ContextualElements[0].Value != null)
                        {
                            var cceValuesJson = Newtonsoft.Json.JsonConvert.SerializeObject(cceResponseFromSession.Components[0].ContextualElements[0].Value);
                            ContextualMessage cceValues = Newtonsoft.Json.JsonConvert.DeserializeObject<ContextualMessage>(cceValuesJson);

                            if (cceValues?.Content?.Links?.Count > 0 && !string.IsNullOrEmpty(cceValues.Content.Links.FirstOrDefault().Link))
                            {
                                response.redirectURL = cceValues.Content.Links.FirstOrDefault().Link;
                            }

                            //make call to feeback
                            await SendChasePromoFeedbackToCCE(chasePromoRedirectRequest.MileagePlusNumber, cceValues.MessageKey, MOBPromoFeedbackEventType.CLICK, (MOBRequest)chasePromoRedirectRequest, chasePromoRedirectRequest.SessionId, "ChasePromoRTIRedirect");
                            //logEntries.AddRange(_shopProductsBusiness.LogEntries);
                        }
                        else
                        {
                            MOBExceptionWrapper unitedEx = new MOBExceptionWrapper();
                            unitedEx.Message = "Invalid Chase promo CCE response in the session";
                            _logger.LogWarning("ChasePromoRTIRedirect {@UnitedException}", JsonConvert.SerializeObject(unitedEx));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("ChasePromoRTIRedirect Error {@Exception}", JsonConvert.SerializeObject(ex));
                    }
                }
                else
                {
                    if (chasePromoRedirectRequest.PromoType.ToUpper() == CHASEADTYPE.PREMIER.ToString())
                    {
                        response.redirectURL = _configuration.GetValue<string>("PremierChaseRedirectURL");
                    }
                    else
                    {
                        response.redirectURL = _configuration.GetValue<string>("NonPremierChaseRedirectURL");
                    }
                }
                response.returnURL = _configuration.GetValue<string>("ChaseApplicationReturnURL");
                String returnURLStrings = _configuration.GetValue<string>("ChaseApplicationReturnURLs");

                if (!String.IsNullOrEmpty(returnURLStrings))
                {
                    List<string> returnUrls = returnURLStrings.Split(',').ToList();
                    response.ReturnURLs = returnUrls;
                }
                else
                {
                    response.ReturnURLs = new List<string>() { response.returnURL };
                }

                if (_configuration.GetValue<bool>("EnableSSOChasePromo"))
                {
                    //response.Token = UtilityHelper.GetSSOToken(chasePromoRedirectRequest.Application.Id, chasePromoRedirectRequest.DeviceId, chasePromoRedirectRequest.Application.Version.Major, chasePromoRedirectRequest.TransactionId, null, chasePromoRedirectRequest.SessionId, chasePromoRedirectRequest.MileagePlusNumber);
                    response.Token =  _dPService.GetSSOTokenString(chasePromoRedirectRequest.Application.Id, chasePromoRedirectRequest.MileagePlusNumber, _configuration);
                    if (!String.IsNullOrEmpty(response.Token))
                    {
                        response.webSessionShareURL = _configuration.GetValue<string>("DotcomSSOUrl");
                        if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
                        {
                            response.redirectURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={response.Token}&landingUrl={response.redirectURL}";
                            response.webSessionShareURL = response.Token = string.Empty;
                        }
                    }

                }
               
            }
            return response;
        }
        private async Task<bool> SendChasePromoFeedbackToCCE(string mileagePlusNumber, string messageKey, MOBPromoFeedbackEventType feedbackEventType, MOBRequest mobRequest, string sessionId, string logAction)
        {
            bool isFeedbackSuccess = false;

            //get cce data from Session
            CCEPromo ccePromoPersist = new CCEPromo();
            var ccePromoFromSession = await _sessionHelperService.GetSession<CCEPromo>(sessionId, ccePromoPersist.ObjectName,new List<string> { sessionId, ccePromoPersist.ObjectName }).ConfigureAwait(false);
            if (ccePromoFromSession == null)
            {
                throw new MOBUnitedException("Could not find the session.");
            }
            var cceResponseFromSession = JsonConvert.DeserializeObject<ContextualCommResponse>(ccePromoFromSession.ContextualCommResponseJson);

            if (cceResponseFromSession != null && cceResponseFromSession.Components.Count > 0
                && cceResponseFromSession.Components[0].ContextualElements.Count > 0
                && cceResponseFromSession.Components[0].ContextualElements[0].Value != null)
            {
                var cceValuesJson = JsonConvert.SerializeObject(cceResponseFromSession.Components[0].ContextualElements[0].Value);
                ContextualMessage cceValues = JsonConvert.DeserializeObject<ContextualMessage>(cceValuesJson);

                if (cceValues != null && ccePromoFromSession != null && ccePromoFromSession.ContextualCommRequest != null)
                {
                    //build request 
                    United.Service.Presentation.PersonalizationRequestModel.PersonalizationFeedbackRequest feedbackRequest = new Service.Presentation.PersonalizationRequestModel.PersonalizationFeedbackRequest();

                    feedbackRequest.LoyaltyProgramMemberID = mileagePlusNumber;
                    feedbackRequest.Requestor = new Service.Presentation.CommonModel.Requestor();
                    feedbackRequest.Requestor.ChannelName = _configuration.GetValue<string>("ChaseBannerCCERequestChannelName");

                    feedbackRequest.Events = new System.Collections.ObjectModel.Collection<FeedbackEvent>();
                    FeedbackEvent feedbackEvent = new FeedbackEvent();
                    feedbackEvent.ChoiceID = cceValues.MessageKey;
                    feedbackEvent.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
                    //feedbackEvent.EventTimeZone  // did not have info from CCE
                    feedbackEvent.Type = feedbackEventType.ToString();
                    feedbackEvent.Page = ccePromoFromSession.ContextualCommRequest.PageToLoad;
                    feedbackEvent.Placement = cceResponseFromSession.Components[0].Name;
                    feedbackEvent.ContextualElement = new ContextualElement();
                    var contextualElementValue = new ContextualMessage();
                    contextualElementValue.MessageKey = cceValues.MessageKey;
                    contextualElementValue.MessageType = cceResponseFromSession.Components[0].Name;
                    contextualElementValue.Params = new Dictionary<string, string>();

                    foreach (var param in cceValues.Params)
                    {
                        contextualElementValue.Params.Add(param.Key, param.Value);
                    }
                    feedbackEvent.ContextualElement.Type = cceResponseFromSession.Components[0].ContextualElements[0].Type;
                    feedbackEvent.ContextualElement.Rank = cceResponseFromSession.Components[0].ContextualElements[0].Rank;
                    feedbackEvent.ContextualElement.Value = contextualElementValue;
                    feedbackRequest.Events.Add(feedbackEvent);

                    //post feedback CCE
                    var jsonRequest = JsonConvert.SerializeObject(feedbackRequest);
                    var session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName,new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
                    var jsonResponse = await _shoppingCcePromoService.ShoppingCcePromo(session.Token, jsonRequest, sessionId).ConfigureAwait(false);
                    var cceResponse = string.IsNullOrEmpty(jsonResponse) ? null
                        : JsonConvert.DeserializeObject<United.Service.Presentation.PersonalizationResponseModel.PersonalizationAcknowledgement>(jsonResponse);
                    //validate 
                    if (cceResponse != null && cceResponse.Response != null)
                    {
                        foreach (var message in cceResponse.Response.Message)
                        {
                            if (message.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
                            {
                                isFeedbackSuccess = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("Invalid Chase promo CCE response in the session");
            }

            return isFeedbackSuccess;
        }
        public async Task<GetProductInfoForFSRDResponse> GetProductInfoForFSRD(GetProductInfoForFSRDRequest getProductInfoForFSRDRequest)
        {
            var response = new GetProductInfoForFSRDResponse();

            //shopping.LogEntries.Add(LogEntry.GetLogEntry(request.SessionId, actionName, "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request));

            if (string.IsNullOrWhiteSpace(getProductInfoForFSRDRequest.SessionId))
                throw new MOBUnitedException("SessionId cannot be empty.");

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(getProductInfoForFSRDRequest.SessionId, session.ObjectName, new List<string> { getProductInfoForFSRDRequest.SessionId, session.ObjectName }).ConfigureAwait(false);
            if (session == null || string.IsNullOrEmpty(session.SessionId) || string.IsNullOrEmpty(session.Token))
            {
                throw new MOBUnitedException("Cannot find your session.");
            }                     
            response = await GetProductInfoForFSRD(session, getProductInfoForFSRDRequest);
            response.TransactionId = getProductInfoForFSRDRequest.TransactionId;
            response.LanguageCode = getProductInfoForFSRDRequest.LanguageCode;

             return response;
        }
        public async Task<GetProductInfoForFSRDResponse> GetProductInfoForFSRD(Session session, GetProductInfoForFSRDRequest request, string shoppingProductCode = "")
        {
            var response = new GetProductInfoForFSRDResponse();
            try
            {
                if (_shoppingUtility.IsEnableGetFSRDInfoFromCSL(request.Application.Id, request.Application.Version.Major.ToString()))
                {
                    var cslResponse = await GetFareColumnEntitlements(session, request);
                    response = await MapFareColumnEntitlements(cslResponse, request, session);
                    return response;
                }
            }            
            catch (System.Net.WebException wex)
            {
                _logger.LogError("GetProductInfoForFSRD - GetFareColumnEntitlements Error {@WebException}", JsonConvert.SerializeObject(wex));
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            catch (MOBUnitedException ex)
            {
                _logger.LogError("GetProductInfoForFSRD Error {@UnitedException}", JsonConvert.SerializeObject(ex));
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductInfoForFSRD Error {@Exception}", JsonConvert.SerializeObject(ex));
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            string requestValidationErrors = ValidateMOBSHOPGetProductInfoForFSRDRequest(request);
            if (!string.IsNullOrWhiteSpace(requestValidationErrors))
                throw new MOBUnitedException(requestValidationErrors);

            BasicEconomyEntitlementResponse ibeEntitlementsResponse = null;
            var ibeBagFee = _configuration.GetValue<string>("IBELiteProdutDefaultPrice");
            var ibeBagFeeCurrency = string.Empty;
            var ecoBagFee = string.Empty;
            ibeEntitlementsResponse = await GetBasicEconomyEntitlements(session, request);
            var success = ValidateBasicEconomyEntitlementResponse(ibeEntitlementsResponse);

            ibeBagFee = GetIBELitePBagFeeAmount(ibeEntitlementsResponse);
            ibeBagFeeCurrency = GetIBELitePBagFeeCurrecy(ibeEntitlementsResponse);
            if (!request.IsIBE)
            {
                ecoBagFee = GetIBELiteEconomyBagFeeAmount(ibeEntitlementsResponse);
            }
            var currencySymbol = !string.IsNullOrEmpty(ibeBagFeeCurrency) ? _shoppingUtility.GetCurrencySymbol(TopHelper.GetCultureInfo(ibeBagFeeCurrency)) : "$";

            var ibeBagFeeOnConfirmFare = !string.IsNullOrEmpty(ibeBagFeeCurrency) ? string.Format("{0}{1}", currencySymbol, ibeBagFee) : _configuration.GetValue<string>("IBELiteProdutDefaultPrice");
            var ibeBagFeeOnCompareFares = !string.IsNullOrEmpty(ibeBagFeeCurrency) ? string.Format("{0}{1} ", currencySymbol, ibeBagFee) : string.Empty;

            if (_configuration.GetValue<bool>("EnableIBE") && request.IsIBE)
            {
                if (!_configuration.GetValue<bool>("EnablePBE"))
                {
                    response.IBELiteShopOptions = await GetIBEFullShoppingOptions();
                    response.IBELiteShopMessages = await GetIBEFullShoppingMessages (request);
                    if (_configuration.GetValue<bool>("BasicEconomyContentChange"))
                    {
                        Model.Shopping.ShoppingResponse shop = new Model.Shopping.ShoppingResponse();
                        try
                        {
                            shop = await _sessionHelperService.GetSession<Model.Shopping.ShoppingResponse>(session.SessionId, shop.ObjectName,new List<string> { session.SessionId, shop.ObjectName }).ConfigureAwait(false);
                            var differentYearcount = shop.Request.Trips.Select(d => Convert.ToDateTime(d.DepartDate).Year).Distinct().Count();
                            if (shop != null && shop.Request.Trips != null && differentYearcount > 1 && DateTime.Now.Year == 2019)
                            {
                                var presentYear = shop.Request.Trips.Where(d => Convert.ToDateTime(d.DepartDate).Year == 2019).FirstOrDefault().DepartDate;
                                var futureYear = shop.Request.Trips.Where(d => Convert.ToDateTime(d.DepartDate).Year == 2020).FirstOrDefault().DepartDate;
                                if (!string.IsNullOrEmpty(presentYear) && !string.IsNullOrEmpty(futureYear))
                                {
                                    if (response.IBELiteShopMessages.Any(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20"))
                                    {
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter");
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020");
                                        (response.IBELiteShopMessages.Where(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                                    }
                                }
                            }
                            else
                            {
                                if (shop != null && shop.Request.Trips != null && Convert.ToDateTime(shop.Request.Trips[0].DepartDate).Year == 2019)
                                {
                                    response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020");
                                    response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20");
                                }
                                else if (shop != null && shop.Request.Trips != null && Convert.ToDateTime(shop.Request.Trips[0].DepartDate).Year >= 2020)
                                {
                                    if (response.IBELiteShopMessages.Any(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020"))
                                    {
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter");
                                        response.IBELiteShopMessages.RemoveAll(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_19_20");
                                        (response.IBELiteShopMessages.Where(t => !string.IsNullOrEmpty(t.CurrentValue) && t.Id == "ELFConfirmFareTypeFooter_2020").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                                    }
                                }
                            }
                        }
                        catch (System.Exception) { }
                    }
                }
            }
            else
            {
                var ecoBagFeeFull = string.Format("{0}{1}", currencySymbol, !string.IsNullOrWhiteSpace(ecoBagFee) ? ecoBagFee : _configuration.GetValue<string>("IBELiteEconomyBagPrice"));

                response.IBELiteShopOptions = await GetIBELiteShoppingOptions(ibeBagFeeOnConfirmFare, string.IsNullOrEmpty(ibeBagFeeCurrency) ? string.Empty : GetIBELiteTripTypeDescription(request.SearchType), ecoBagFeeFull);
                response.IBELiteShopMessages = await GetIBELiteShoppingMessages(request);
            }
            response.ShoppingProducts = await GetIBELiteShoppingProducts(request, ibeBagFeeOnCompareFares, GetIBELiteTripTypeDescription(request.SearchType));

            if (_configuration.GetValue<bool>("EnablePBE"))
            {
                string productCode = string.IsNullOrEmpty(shoppingProductCode)
                                         ? response?.ShoppingProducts?.First(p => p != null && p.IsIBE).ProductCode
                                         : shoppingProductCode;
                response.IBELiteShopOptions = await GetIBEFullShoppingOptions (productCode);
                if (_shoppingUtility.EnableOptoutScreenHyperlinkSupportedContent(request.Application.Id, request.Application.Version.Major))
                {
                    response.IBELiteShopMessages = GetIBEFullShoppingMessages(request, productCode, productCode + "_CONFIRMATION_PAGE_HEADER_FOOTER_V1");
                }
                else
                {
                    response.IBELiteShopMessages = await GetIBEFullShoppingMessages(request, productCode);
                }
            }

            return response;
        }
        private string ValidateMOBSHOPGetProductInfoForFSRDRequest(GetProductInfoForFSRDRequest request)
        {
            string errorMsg = null;
            var kv = new Dictionary<string, string>
            {
                { "SessionId", request.SessionId },
                { "CountryCode", request.CountryCode },
                { "FlightHash", request.FlightHash },
                { "SearchType", request.SearchType }
            };

            var invalidValue = kv.FirstOrDefault(pair => string.IsNullOrWhiteSpace(pair.Value));
            if (!invalidValue.Equals(default(KeyValuePair<string, string>)))
                errorMsg = string.Format("Request's {0} is empty.", invalidValue.Key);
            else if ("RT,OW,MD".IndexOf(request.SearchType.Trim().ToUpper()) < 0)
                errorMsg = string.Format("Request's SearchType is invalid.");

            return errorMsg;
        }
        private async Task<BasicEconomyEntitlementResponse> GetBasicEconomyEntitlements(Session session, GetProductInfoForFSRDRequest request)
        {
            string cslActionName = "/GetBasicEconomyEntitlements";
            var cslRequest = GetBasicEconomyEntitlementRequest(session, request);
            string cslRequestJson = JsonConvert.SerializeObject(cslRequest);

            var jsonResponse = await _flightShoppingService.GetEconomyEntitlement(session.Token, cslActionName, cslRequestJson, session.SessionId).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var response = JsonConvert.DeserializeObject<BasicEconomyEntitlementResponse>(jsonResponse);

                if (response != null && !response.Status.Equals(StatusType.Success))
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        var builtErrMsg = string.Join(", ", response.Errors.Select(x => x.Message));

                        throw new Exception(builtErrMsg);
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private async Task<FareColumnEntitlementResponse> GetFareColumnEntitlements(Session session, GetProductInfoForFSRDRequest request)
        {
            string cslActionName = "FareColumnEntitlements";

            var cslRequest = GetFareColumnEntitlementsRequest(session?.CartId, request?.LanguageCode);
            string cslRequestJson = JsonConvert.SerializeObject(cslRequest);

            var jsonResponse = await _flightShoppingService.GetFareColumnEntitlements(session.Token, cslActionName, cslRequestJson, session.SessionId).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var response = JsonConvert.DeserializeObject<FareColumnEntitlementResponse>(jsonResponse);

                if (response != null && !response.Status.Equals(StatusType.Success))
                {
                    if (response != null && response.Errors != null && response.Errors.Count > 0)
                    {
                        var builtErrMsg = string.Join(", ", response.Errors.Select(x => x.Message));

                        throw new Exception(builtErrMsg);
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private FareColumnEntitlementsRequest GetFareColumnEntitlementsRequest(string cartId, string langCode)
        {
            return new FareColumnEntitlementsRequest
            {
                CartId = cartId,
                LangCode = langCode
            };
        }

        private async Task<GetProductInfoForFSRDResponse> MapFareColumnEntitlements(FareColumnEntitlementResponse cslResponse, GetProductInfoForFSRDRequest request, Session session)
        {
            var response = new GetProductInfoForFSRDResponse();
            response.IBELiteShopMessages = new List<MOBItem>();
            response.IBELiteShopOptions = new List<Option>();
            response.Legends = new List<MOBItemWithIconName>();

            if (cslResponse?.Entitlements?.Count > 0 && cslResponse.FootNotes?.Count > 0)
            {
                List<CMSContentMessage> lstMessages
                                    = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token,
                                    _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"),
                                    "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                foreach (var entitlement in cslResponse?.Entitlements)
                {
                    if (entitlement != null && entitlement.Info != null && !string.IsNullOrEmpty(entitlement.Icon) && !string.IsNullOrEmpty(entitlement?.Title?.Key))
                    {
                        Option.MOBOptionsForBEAndEconomy optionForBE = Option.MOBOptionsForBEAndEconomy.checkmark;
                        Option.MOBOptionsForBEAndEconomy optionForEconomy = Option.MOBOptionsForBEAndEconomy.checkmark;

                        foreach (var info in entitlement.Info)
                        {
                            if (info != null && !string.IsNullOrEmpty(info.FareType) && !string.IsNullOrEmpty(info.Icon))
                            {
                                if (info.FareType.ToUpper() == "ECO-BASIC")
                                {
                                    optionForBE = MapOptionsForBEAndEconomy(info.Icon);
                                }
                                else if (info.FareType.ToUpper() == "ECONOMY")
                                {
                                    optionForEconomy = MapOptionsForBEAndEconomy(info.Icon);
                                }
                            }
                        }

                        var optionIcon = MapOptionIcon(entitlement.Icon.Trim().ToLower());
                        var option = new Option()
                        {
                            OptionDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, entitlement.Title.Key + "_Mobile"),
                            OptionIcon = optionIcon,
                            OptionForBE = optionForBE,
                            OptionForEconomy = optionForEconomy,
                            AvailableInElf = (optionForBE == MOBOptionsForBEAndEconomy.checkmark),
                            AvailableInEconomy = (optionForEconomy == MOBOptionsForBEAndEconomy.checkmark)
                        };
                        response.IBELiteShopOptions.Add(option);
                    }
                }

                var bEConfirmFareTypeInfoList = _configuration.GetValue<string>("BEConfirmFareTypeInfoList").Split('|');
                if (bEConfirmFareTypeInfoList != null && bEConfirmFareTypeInfoList.Length > 0)
                {
                    foreach (string bEConfirmFareTypeInfo in bEConfirmFareTypeInfoList)
                    {
                        var currentValue = "";
                        if (!string.IsNullOrEmpty(bEConfirmFareTypeInfo) && bEConfirmFareTypeInfo.Trim() == "ELFConfirmFareTypeFooter")
                        {
                            cslResponse.FootNotes.ForEach(f => currentValue = currentValue + _shoppingUtility.GetSDLStringMessageFromList(lstMessages, f?.Message?.Key + "_Mobile") + "<br /><br />");

                            currentValue = "<span style=\"color: #666666;\"><b>With Basic Economy:</b><br /><br />" + currentValue + "</span>";
                        }
                        else if (!string.IsNullOrEmpty(bEConfirmFareTypeInfo))
                        {
                            currentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, bEConfirmFareTypeInfo + "_Mobile");
                        }
                        response.IBELiteShopMessages.Add(new MOBItem { Id = bEConfirmFareTypeInfo, CurrentValue = currentValue, SaveToPersist = false });
                    }
                }
                bool isDynamiclegendenable = await _shoppingUtility.IsDynamicLegendsEnabled(request.Application.Id, request.Application.Version.Major.ToString());
                if (isDynamiclegendenable && cslResponse?.Legends?.Count > 0)
                {                                        
                    foreach (var bELegends in cslResponse.Legends)
                    {
                        var currentLegend = new MOBItemWithIconName()
                        {
                            OptionDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, bELegends?.Description?.Key + "_MOBILE"),
                            OptionIcon = bELegends.Icon,
                        };                                             
                        response.Legends.Add(currentLegend);
                    }                   
                }
            }
            return response;
        }

        private string MapOptionIcon(string icon)
        {
            string optionIcon = "";
            switch (icon)
            {
                case "legroom-standard":
                    optionIcon = "be_seat";
                    break;
                case "traveler":
                    optionIcon = "be_traveler";
                    break;
                case "luggage":
                    optionIcon = "be_bag";
                    break;
                case "baggage":
                    optionIcon = "baggage";
                    break;
                case "change":
                    optionIcon = "be_flight";
                    break;
                case "mileageplus":
                    optionIcon = "be_mp";
                    break;
            }

            return optionIcon;
        }

        private MOBOptionsForBEAndEconomy MapOptionsForBEAndEconomy(string icon)
        {
            switch (icon)
            {
                case "checkmark":
                    return MOBOptionsForBEAndEconomy.checkmark;
                case "close":
                    return MOBOptionsForBEAndEconomy.close;
                case "circle-money":
                case "price":
                    return MOBOptionsForBEAndEconomy.price;
            }
            return default;
        }

        private BasicEconomyEntitlementRequest GetBasicEconomyEntitlementRequest(Session session, GetProductInfoForFSRDRequest request)
        {
            return new BasicEconomyEntitlementRequest
            {
                CartId = session.CartId,
                CountryCode = request.CountryCode,
                FlightHash = request.FlightHash,
                LangCode = request.LanguageCode,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType")
            };
        }
        private bool ValidateBasicEconomyEntitlementResponse(BasicEconomyEntitlementResponse ibeEntitlementsResponse)
        {
            if (ibeEntitlementsResponse.Status == StatusType.Success)
                return true;

            if (ibeEntitlementsResponse.Errors == null || !ibeEntitlementsResponse.Errors.Any())
                throw new MOBUnitedException("Unable to get bag fee.");

            throw new MOBUnitedException(string.Join(", ", ibeEntitlementsResponse.Errors.Select(x => x.Message)));
        }
        private string GetIBELitePBagFeeAmount(BasicEconomyEntitlementResponse cslRepsonse)
        {
            if (cslRepsonse.Status != StatusType.Success)
                throw new MOBUnitedException("Bad Response");

            return cslRepsonse.ProductOffer.Offers.First().ProductInformation.ProductDetails.First().Product.SubProducts.First().Prices.First().PaymentOptions.First().PriceComponents.First().Price.Totals.First().Amount.ToString();
        }
        private string GetIBELitePBagFeeCurrecy(BasicEconomyEntitlementResponse cslRepsonse)
        {
            if (cslRepsonse.Status != StatusType.Success)
                throw new MOBUnitedException("Bad Response");

            return cslRepsonse.ProductOffer.Offers.First().ProductInformation.ProductDetails.First().Product.SubProducts.First().Prices.First().PaymentOptions.First().PriceComponents.First().Price.Totals.First().Currency.Code;
        }
        private string GetIBELiteEconomyBagFeeAmount(BasicEconomyEntitlementResponse cslRepsonse)
        {
            if (cslRepsonse.Status != StatusType.Success)
                throw new MOBUnitedException("Bad Response");

            return cslRepsonse.ProductOffer.Offers.First().ProductInformation.ProductDetails.First().Product.SubProducts.First().Extension.Bag.Bags.First().RegularPrice.First().Price.Totals.First().Amount.ToString();
        }
        private async Task<List<Option>> GetIBEFullShoppingOptions()
        {
            return (await GetIBEFullConfirmFarePageDocs())
                            .Where(o => o != null)
                            .OrderBy(o => Convert.ToInt32(o.Title))
                            .Select(o => o.LegalDocument.Split('|'))
                            .Select(doc => new Option
                            {
                                OptionDescription = doc[0],
                                AvailableInElf = Convert.ToBoolean(doc[1]),
                                AvailableInEconomy = Convert.ToBoolean(doc[2]),
                                fareSubDescriptionELF = null,
                                fareSubDescriptionEconomy = null,
                                OptionIcon = doc[5]
                            })
                            .ToList();
        }
        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmFarePageDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("IBEFULL_CONFIRMATION_PAGE_OPTIONS" , _headers.ContextValues.SessionId,true).ConfigureAwait(false);
        }
        private async Task<List<MOBItem>> GetIBEFullShoppingMessages(GetProductInfoForFSRDRequest request)
        {
            var msgs = (await GetIBEFullConfirmPageHeaderFooterDocs())
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }
        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmPageHeaderFooterDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles("IBEFULL_CONFIRMATION_PAGE_HEADER_FOOTER" , _headers.ContextValues.SessionId,true).ConfigureAwait(false);
        }
        private async Task<List<Option>> GetIBELiteShoppingOptions(string ibeBagFee, string ibeTripTypeDesc, string ecoBagFee)
        {
            return (await GetIBELiteConfirmFarePageDocs())
                            .Where(o => o != null)
                            .OrderBy(o => Convert.ToInt32(o.Title))
                            .Select(o => o.LegalDocument.Split('|'))
                            .Select(doc => new Option
                            {
                                OptionDescription = doc[0],
                                AvailableInElf = Convert.ToBoolean(doc[1]),
                                AvailableInEconomy = Convert.ToBoolean(doc[2]),
                                fareSubDescriptionELF = string.IsNullOrWhiteSpace(doc[3]) ? null : string.Format("{0} {1}", ibeBagFee, ibeTripTypeDesc),
                                fareSubDescriptionEconomy = string.IsNullOrWhiteSpace(doc[4]) ? null : ecoBagFee,
                                OptionIcon = doc[5]
                            })
                            .ToList();
        }
        private async Task<List<MOBLegalDocument>> GetIBELiteConfirmFarePageDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles( "IBELITE_CONFIRMATION_PAGE_OPTIONS" , _headers.ContextValues.SessionId,true).ConfigureAwait(false);
        }
        private async Task<List<MOBItem>> GetIBELiteShoppingMessages(GetProductInfoForFSRDRequest request)
        {
            var msgs = (await GetIBELiteConfirmPageHeaderFooterDocs())
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }
        private async Task<List<MOBLegalDocument>> GetIBELiteConfirmPageHeaderFooterDocs()
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles( "IBELITE_CONFIRMATION_PAGE_HEADER_FOOTER" , _headers.ContextValues.SessionId,true).ConfigureAwait(false);
        }
        private async Task<List<Model.Shopping.MOBSHOPShoppingProduct>> GetIBELiteShoppingProducts(GetProductInfoForFSRDRequest request, string ibeBagFee, string ibeTripTypeDesc)
        {
            if (string.IsNullOrEmpty(request.TripId))
                return null;

            var persistedAvail = await GetLastTripAvailabilityFromPersist(request.TripId, request.SessionId);

            List<Model.Shopping.MOBSHOPFlattenedFlight> shopFlattenedFlightList = null;
            if (persistedAvail == null || persistedAvail.Trip.LastTripIndexRequested == 1)
            {
                var persistShopFlattenedFlightList = new MOBSHOPFlattenedFlightList();
                persistShopFlattenedFlightList = await _sessionHelperService.GetSession<MOBSHOPFlattenedFlightList>(request.SessionId, persistShopFlattenedFlightList.ObjectName,new List<string> { request.SessionId, persistShopFlattenedFlightList.ObjectName }).ConfigureAwait(false);

                if (persistShopFlattenedFlightList != null)
                    shopFlattenedFlightList = persistShopFlattenedFlightList.FlattenedFlightList;

                if (shopFlattenedFlightList == null || shopFlattenedFlightList.Count == 0)
                {
                    shopFlattenedFlightList = await GetFlattendFlightsFromAvailability(request.SessionId);
                }
            }

            if (shopFlattenedFlightList == null && persistedAvail == null)
                return null;

            if (shopFlattenedFlightList == null)
                shopFlattenedFlightList = persistedAvail.Trip.FlattenedFlights;
            
             var products = shopFlattenedFlightList.First(f => f.FlightHash!=null && f.FlightHash.Equals(request.FlightHash, StringComparison.OrdinalIgnoreCase))
                                .Flights
                                .First()
                                .ShoppingProducts;

            if (_configuration.GetValue<bool>("EnableIBE") && request.IsIBE)
            {
                var ibeProducts = string.Format(HttpUtility.HtmlDecode(_configuration.GetValue<string>("IBEFulldetails")), ibeBagFee, ibeTripTypeDesc);
                products.First().ProductDetail.ProductDetails = ibeProducts.Split('|').ToList();
            }
            else
            {
                var ibeProducts = string.Format(string.Join("^", products.First().ProductDetail.ProductDetails), ibeBagFee, ibeTripTypeDesc);
                products.First().ProductDetail.ProductDetails = ibeProducts.Split('^').ToList();
            }

            return products;
        }
        public async Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(string tripID, string sessionID)
        {
            MOBSHOPAvailability lastTripAvailability = null;

            if (string.IsNullOrWhiteSpace(tripID))
                return lastTripAvailability;

            var persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName }).ConfigureAwait(false);

            if (persistAvailability != null && persistAvailability.AvailabilityList != null && persistAvailability.AvailabilityList.Any())
            {
                lastTripAvailability = persistAvailability.AvailabilityList.Values.FirstOrDefault(x => x.Trip.TripId.Equals(tripID, StringComparison.OrdinalIgnoreCase));
            }

            return lastTripAvailability;
        }
        private async Task<List<Model.Shopping.MOBSHOPFlattenedFlight>> GetFlattendFlightsFromAvailability(string sessionID)
        {
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName }).ConfigureAwait(false);
            var persistFFlights = persistAvailability.AvailabilityList[persistAvailability.AvailabilityList.Count.ToString()].Trip.FlattenedFlights;

            return persistFFlights;
        }
        internal string GetIBELiteTripTypeDescription(string searchType)
        {
            if (string.IsNullOrEmpty(searchType))
                return string.Empty;

            string tripTypeDesc = string.Empty;
            switch (searchType.ToUpper().Trim())
            {
                case "RT":
                    tripTypeDesc = "(roundtrip)";
                    break;
                case "OW":
                    tripTypeDesc = "(one way)";
                    break;
                case "MD":
                    tripTypeDesc = "(each way)";
                    break;
                default:
                    break;
            }

            return tripTypeDesc;
        }
        private async Task<List<Option>> GetIBEFullShoppingOptions(string productCode)
        {
            return (await GetIBEFullConfirmFarePageDocs(productCode))
                            .Where(o => o != null)
                            .OrderBy(o => Convert.ToInt32(o.Title))
                            .Select(o => o.LegalDocument.Split('|'))
                            .Select(doc => new Option
                            {
                                OptionDescription = doc[0],
                                AvailableInElf = Convert.ToBoolean(doc[1]),
                                AvailableInEconomy = Convert.ToBoolean(doc[2]),
                                fareSubDescriptionELF = null,
                                fareSubDescriptionEconomy = null,
                                OptionIcon = doc[5]
                            })
                            .ToList();
        }
        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmFarePageDocs(string productCode)
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(productCode + "_CONFIRMATION_PAGE_OPTIONS" , _headers.ContextValues.TransactionId,true).ConfigureAwait(false);
        }
        private List<MOBItem> GetIBEFullShoppingMessages(GetProductInfoForFSRDRequest request, string productCode, string title)
        {
            var msgs = _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles( title , _headers.ContextValues.TransactionId,true).Result
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }
        private async Task<List<MOBItem>> GetIBEFullShoppingMessages(GetProductInfoForFSRDRequest request, string productCode)
        {
            var msgs = (await GetIBEFullConfirmPageHeaderFooterDocs(productCode))
                          .Select(doc => new MOBItem { Id = doc.Title, CurrentValue = doc.LegalDocument })
                          .ToList();

            if (request.NumberOfAdults == 1)
            {
                var elfConfirmFareTypeTitle = msgs.FirstOrDefault(msg => msg != null && msg.Id.Equals("ELFConfirmFareTypeTitle", StringComparison.OrdinalIgnoreCase));

                if (elfConfirmFareTypeTitle != null)
                    elfConfirmFareTypeTitle.CurrentValue = string.Empty;
            }

            return msgs;
        }
        private async Task<List<MOBLegalDocument>> GetIBEFullConfirmPageHeaderFooterDocs(string productCode)
        {
            return await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(productCode + "_CONFIRMATION_PAGE_HEADER_FOOTER", _headers.ContextValues.SessionId,true).ConfigureAwait(false);
        }

    }

}
