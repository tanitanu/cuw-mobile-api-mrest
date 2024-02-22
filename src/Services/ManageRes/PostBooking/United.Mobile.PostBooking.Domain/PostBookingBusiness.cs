using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.PostBooking;
using United.Mobile.Model.Shopping.Pcu;
using United.Utility.Helper;

namespace United.Mobile.PostBooking.Domain
{
    public class PostBookingBusiness : IPostBookingBusiness
    {
        private readonly ICacheLog<PostBookingBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IFeatureToggles _featureToggles;
        private readonly IFFCShoppingcs _ffcShopping;
        public PostBookingBusiness(ICacheLog<PostBookingBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;           
        }

        public async Task<MOBSHOPGetOffersResponse> GetOffers(MOBSHOPGetOffersRequest request)
        {
            var response = new MOBSHOPGetOffersResponse();
            //if (traceSwitch.TraceInfo)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry(request.SessionId, "GetOffers", "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request, isJSONSave: true, LogIt: false));
            //}
            _logger.LogInformation("GetOffers {SessionId}, {ApplicationId}, {AppVersion}, {DeviceId} and {Request}", request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, request);
            try
            {
                response.Request = request;
                response.SessionId = request.SessionId;
                var session = new Session();
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);

                if (IsProductCodeRequested(request.ProductCodes, "PCU"))
                {
                    if (_configuration.GetValue<bool>("EnablePCUatReshop") || session != null && !session.IsReshopChange || request.IsFromViewResSeatMap)
                    {
                        var pcuState = new PcuState();
                        pcuState = await _sessionHelperService.GetSession<PcuState>(request.SessionId, pcuState.ObjectName, new List<string> { request.SessionId, pcuState.ObjectName }).ConfigureAwait(false);
                        response.PremiumCabinUpgrade = pcuState != null
                                                       ? pcuState.PremiumCabinUpgradeOfferDetail
                                                       : null;
                        if (response.PremiumCabinUpgrade == null && request.IsFromViewResSeatMap)
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("UpgradesPageUnavailableException"));
                        }
                    }
                }

                if (IsProductCodeRequested(request.ProductCodes, "PBS"))
                {
                    if (_configuration.GetValue<bool>("IsPBSLazyLoadingEnabled") || session != null && !session.IsReshopChange) //pb for postbooking and reshop not for view reservation
                    {
                        PriorityBoardingFile persistedPriorityBoarding = new PriorityBoardingFile();
                        persistedPriorityBoarding =await _sessionHelperService.GetSession<PriorityBoardingFile>(request.SessionId, persistedPriorityBoarding.ObjectName, new List<string> { request.SessionId, persistedPriorityBoarding.ObjectName }).ConfigureAwait(false);
                        response.PriorityBoarding = persistedPriorityBoarding != null
                                                       ? persistedPriorityBoarding.PriorityBoarding
                                                       : null;
                        if (response.PriorityBoarding == null)
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("UpgradesPageUnavailableException"));
                        }
                    }
                }
             
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetOffers Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(uaex), request.TransactionId);
                _logger.LogWarning("GetOffers Error {exception} and {sessionId}", uaex.Message, request.TransactionId);
               
            }
            catch (Exception ex)
            {
                _logger.LogWarning("GetOffers Error {exceptionstack} and {sessionId}", JsonConvert.SerializeObject(ex), request.TransactionId);
                _logger.LogWarning("GetOffers Error {exception} and {sessionId}", ex.Message, request.TransactionId);
            }

            return await Task.FromResult(response);
        }

        private bool IsProductCodeRequested(string requestedProductCodes, string productCode)
        {
            if (string.IsNullOrWhiteSpace(requestedProductCodes) || string.IsNullOrWhiteSpace(productCode))
                return false;

            return requestedProductCodes.Split(',').Any(p => p.Equals(productCode, StringComparison.OrdinalIgnoreCase));
        }
    
    }
}
