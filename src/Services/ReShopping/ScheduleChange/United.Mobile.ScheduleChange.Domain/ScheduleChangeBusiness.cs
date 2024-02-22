using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.ScheduleChange;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.ReShop;
using United.Utility.Enum;
using United.Utility.Helper;

namespace United.Mobile.ScheduleChange.Domain
{
    public class ScheduleChangeBusiness : IScheduleChangeBusiness
    {
        private readonly ICacheLog<ScheduleChangeBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IReservationService _reservationService;
        private readonly IRecordLocatorBusiness _recordLocatorBusiness;

        public ScheduleChangeBusiness(ICacheLog<ScheduleChangeBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IReservationService reservationService
            , IRecordLocatorBusiness recordLocatorBusiness)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _reservationService = reservationService;
            _recordLocatorBusiness = recordLocatorBusiness;
        }

        public async Task<MOBConfirmScheduleChangeResponse> ConfirmScheduleChange(MOBConfirmScheduleChangeRequest request)
        {
            Session session = null;

            var response = new MOBConfirmScheduleChangeResponse();

            _logger.LogInformation("ConfirmScheduleChange {clientRequest} and {SessionId}", request, request.SessionId);

            if (!string.IsNullOrEmpty(request.SessionId))
            {
                session = await _sessionHelperService.GetSession<Session>(Headers.ContextValues.SessionId, session.ObjectName, new List<string>() { Headers.ContextValues.SessionId, session.ObjectName });

                //session = Utility.CreateShoppingSession
                //    (request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId,
                //    null, new List<LogEntry>(), _traceSwitch, string.Empty);
            }
            if (session == null)
            {
                response.Exception.Message = _configuration.GetValue<string>("GeneralSessionExpiryMessage");
                response.Exception.Code = "9999";
                return response;
            }

            request.Token = session.Token;
            response.SessionId = request.SessionId;

            response = await ConfirmScheduleChangeCSL(request);

            if (response.Exception == null)
            {
                var mobPnrRequest = new MOBPNRByRecordLocatorRequest();

                //GetPNRByRecordLocator - Request Mapping
                mobPnrRequest.Application = new MOBApplication();
                mobPnrRequest.Application = request.Application;
                mobPnrRequest.SessionId = request.SessionId;
                mobPnrRequest.DeviceId = request.DeviceId;
                mobPnrRequest.TransactionId = request.TransactionId;
                mobPnrRequest.RecordLocator = request.RecordLocator;
                mobPnrRequest.LastName = request.LastName;
                mobPnrRequest.MileagePlusNumber = request.MileagePlusNumber;
                mobPnrRequest.HashKey = request.HashKey;
                mobPnrRequest.Flow = Convert.ToString(FlowType.VIEWRES);

                response.PNRResponse = _recordLocatorBusiness.GetPNRByRecordLocator(mobPnrRequest);
                response.SessionId = response.PNRResponse?.SessionId;
            }

            response.RecordLocator = request.RecordLocator;
            response.LastName = response.LastName;
            response.MileagePlusNumber = request.MileagePlusNumber;
            response.FlowType = request.FlowType;
            response.DeviceId = request.DeviceId;
            response.TransactionId = request.TransactionId;
            response.SelectedOption = request.SelectedOption;

            _logger.LogInformation("ConfirmScheduleChange {clientResponse} and {SessionId}", response, request.SessionId);
            return response;
        }

        public async Task<MOBConfirmScheduleChangeResponse> ConfirmScheduleChangeCSL(MOBConfirmScheduleChangeRequest schedulechangerequest)
        {
            var schedulechangeresponse = new MOBConfirmScheduleChangeResponse();

            try
            {
                var cslResponse = await _reservationService.ConfirmScheduleChange<List<United.Service.Presentation.CommonModel.Message>>(schedulechangerequest.Token, schedulechangerequest.RecordLocator, schedulechangerequest.SessionId);

                if (!string.IsNullOrEmpty(cslResponse))
                {
                    var cslResponseString = JsonConvert.DeserializeObject<List<United.Service.Presentation.CommonModel.Message>>(cslResponse);
                    if (cslResponseString == null || !cslResponseString.Any())
                    {
                        schedulechangeresponse.Exception
                        = new MOBException("9999", _configuration.GetValue<string>("PNRConfmScheduleChangeExcMessage"));
                    }
                }
            }
            catch (Exception e)
            {
                string errormessage = TopHelper.ExceptionMessages(e);
                throw new UnitedException(errormessage);
            }

            return schedulechangeresponse;
        }

    }
}
