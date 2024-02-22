using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.MoneyPlusMiles;
using United.Utility.Helper;

namespace United.Mobile.Services.Shopping.Domain
{
    public class ShopMileagePricingBusiness : IShopMileagePricingBusiness
    {
        private readonly ICacheLog<ShopMileagePricingBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IMileagePricingService _mileagePricingService;
        private readonly IShopBooking _shopBooking;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;

        public ShopMileagePricingBusiness(
            IConfiguration configuration, 
            ISessionHelperService sessionHelperService, 
            IMileagePricingService mileagePricingService,
            ICacheLog<ShopMileagePricingBusiness> logger,
            IHeaders headers,
            IShopBooking shopBooking, 
            IShoppingUtility shoppingUtility,
            IFFCShoppingcs fFCShoppingcs)
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _mileagePricingService = mileagePricingService;
            _logger = logger;
            _headers = headers;
            _shopBooking = shopBooking;
            _shoppingUtility = shoppingUtility;
            _fFCShoppingcs = fFCShoppingcs;
        }

        public async Task<MOBMoneyPlusMilesOptionsResponse> GetMoneyPlusMilesOptions(MOBMoneyPlusMilesOptionsRequest request, HttpContext httpContext)
        {
            MOBMoneyPlusMilesOptionsResponse response = new MOBMoneyPlusMilesOptionsResponse();
            try
            {
                Session session = new Session();
                MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
                shopRequest = await _sessionHelperService.GetSession<MOBSHOPShopRequest>(request?.SessionId, shopRequest.ObjectName, new List<string> { request?.SessionId, shopRequest.ObjectName }).ConfigureAwait(false);
                if (ValidateMoneyPlusMilesEligiblity(shopRequest, session))
                {
                    response = await _mileagePricingService.GetMoneyPlusMilesOptions(session, request);
                    
                    if (response?.Flights == null)
                    {
                        response.Exception = new MOBException("7390", _configuration.GetValue<string>("FSRMoneyPlusMilesUnavailableMessage"));
                    }
                    else
                    {
                        session.IsEligibleForFSRMoneyPlusMiles = response.Flights?.Count > 0;
                        session.IsMoneyPlusMilesSelected = false; 
                        if (session.MileagPlusNumber?.ToUpper().Trim() != request.MileagePlusAccountNumber?.ToUpper().Trim())
                        {
                            session.MileagPlusNumber = request.MileagePlusAccountNumber;
                        }
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(request.CurrentPricingType) && request.CurrentPricingType.Equals(PricingType.ETC.ToString()))
                        {
                            if (response.OnScreenAlerts == null)
                            {
                                response.OnScreenAlerts = new List<MOBOnScreenAlert>();
                            }
                            response.OnScreenAlerts.Add(ETCMoneyPlusMilesAlertMessage(MOBOnScreenAlertType.FSRMILEAGEPRICINGMONEYPLUSMILES));
                        }
                    }
                    
                }
                else
                {
                    response.Exception = new MOBException("7390", _configuration.GetValue<string>("FSRMoneyPlusMilesUnavailableMessage"));
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetMoneyPlusMilesOptions Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                if (uaex.Message == "10038")
                {
                   
                }

                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                   response.Exception.Code = "7390";
            }
            catch (Exception ex)
            {
                _logger.LogError("GetMoneyPlusMilesOptions Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("7390", _configuration.GetValue<string>("VulnerabilityErrorMessage"));
                
            }
            return await Task.FromResult(response);
        }

        public bool ValidateMoneyPlusMilesEligiblity(MOBSHOPShopRequest request, Session session)
            {
                    return (request != null && !(request.IsReshop || request.IsReshopChange
                    || request.IsCorporateBooking 
                    || request.AwardTravel));
            }        

        public async Task<MOBFSRMileagePricingResponse> ApplyMileagePricing(MOBFSRMileagePricingRequest request, HttpContext httpContext)
        {
            MOBFSRMileagePricingResponse response = new MOBFSRMileagePricingResponse();
            try
            {
                Session session = new Session();
                MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
                shopRequest = await _sessionHelperService.GetSession<MOBSHOPShopRequest>(request?.SessionId, shopRequest.ObjectName, new List<string> { request?.SessionId, shopRequest.ObjectName }).ConfigureAwait(false);
                if (ValidateApplyMileagePricing(request, session))
                {
                    response = await _mileagePricingService.GetApplyMileagePricing(session, request);
                    response.PricingType = request.PricingType;
                    if (response?.Flights == null)
                    {
                        response.Exception = new MOBException("7390", _configuration.GetValue<string>("FSRMileagePricingUnavailableMessage"));
                    }
                    else
                    {
                        List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                        session.IsEligibleForFSRPricingType = true;
                        session.PricingType = request.PricingType;
                        session.CreditsAmount = request.CreditsAmount;
                        if (session.MileagPlusNumber?.ToUpper().Trim() != request.MileagePlusAccountNumber?.ToUpper().Trim())
                        {
                            session.MileagPlusNumber = request.MileagePlusAccountNumber;
                        }
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);

                        response.FsrAlertMessages = await _shoppingUtility.SetFSRTravelTypeAlertMessage(session, lstMessages);
                  
                        if (!string.IsNullOrEmpty(request.CurrentPricingType) && request.CurrentPricingType.Equals(PricingType.MONEYPLUSMILES.ToString()))
                        {
                            if(response.OnScreenAlerts == null)
                            {
                                response.OnScreenAlerts = new List<MOBOnScreenAlert>();
                            }
                            response.OnScreenAlerts.Add(ETCMoneyPlusMilesAlertMessage(MOBOnScreenAlertType.FSRMONEYPLUSMILESMILEAGEPRICING, lstMessages));
                        }
                    }

                }
                else
                {
                    response.Exception = new MOBException("7390", _configuration.GetValue<string>("FSRMileagePricingUnavailableMessage"));
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("ApplyMileagePricing Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                if (uaex.Message == "10038")
                {

                }

                response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                response.Exception.Code = "7390";
            }
            catch (Exception ex)
            {
                _logger.LogError("ApplyMileagePricing Error {@Exception}", JsonConvert.SerializeObject(ex));
                response.Exception = new MOBException("7390", _configuration.GetValue<string>("VulnerabilityErrorMessage"));

            }
            return await Task.FromResult(response);
        }

        private MOBOnScreenAlert ETCMoneyPlusMilesAlertMessage(MOBOnScreenAlertType mOBOnScreenAlertType, List<CMSContentMessage> lstMessages = null)
        {
            MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert { Actions = new List<MOBOnScreenActions>() };
            mobOnScreenAlert.Title = _configuration.GetValue<string>("UntiedOnScreenAlertTitle"); 
            mobOnScreenAlert.AlertType = mOBOnScreenAlertType;
            string alerMessage = "";
            if (string.IsNullOrEmpty(alerMessage) == false)
                mobOnScreenAlert.Message = alerMessage;
            else
            {
                if (lstMessages != null && lstMessages.Any(x => x.LocationCode?.Equals(_configuration.GetValue<string>("SDLFSRMPMETCCreditAlertTitle")) ?? false) && lstMessages.Any(x => x.LocationCode?.Equals(_configuration.GetValue<string>("SDLFSRETCCreditsMoneyPlusMilesAlertTitle")) ?? false))
                {
                    if (mOBOnScreenAlertType == MOBOnScreenAlertType.FSRMONEYPLUSMILESMILEAGEPRICING)
                    {
                        mobOnScreenAlert.Title = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("SDLFSRMPMETCCreditAlertTitle"));
                        mobOnScreenAlert.Message = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("SDLFSRMPMETCCreditAlertDescription"));
                    }
                    else if (mOBOnScreenAlertType == MOBOnScreenAlertType.FSRMILEAGEPRICINGMONEYPLUSMILES)
                    {
                        mobOnScreenAlert.Title = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("SDLFSRETCCreditsMoneyPlusMilesAlertTitle"));
                        mobOnScreenAlert.Message = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("SDLFSRETCCreditsMoneyPlusMilesAlertDescription"));
                    }
                }
                else
                {
                    if (mOBOnScreenAlertType == MOBOnScreenAlertType.FSRMONEYPLUSMILESMILEAGEPRICING)
                    {
                        mobOnScreenAlert.Title = _configuration.GetValue<string>("FSRMoneyPlusMilesETCCreditAlertTitle");
                        mobOnScreenAlert.Message = _configuration.GetValue<string>("FSRMoneyPlusMilesETCCreditAlertDescription");
                    }
                    else if (mOBOnScreenAlertType == MOBOnScreenAlertType.FSRMILEAGEPRICINGMONEYPLUSMILES)
                    {
                        mobOnScreenAlert.Title = _configuration.GetValue<string>("FSRETCCreditMoneyPlusMilesAlertTitle");
                        mobOnScreenAlert.Message = _configuration.GetValue<string>("FSRETCCreditMoneyPlusMilesAlertDescription");
                    }
                }
            }
            mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = "Continue",
                ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
            });

            return mobOnScreenAlert;
        }

        public bool ValidateApplyMileagePricing(MOBFSRMileagePricingRequest request, Session session)
        {
            if (request != null && !string.IsNullOrEmpty(request.PricingType))
                return true;
            else return false;
        }

    }

}
