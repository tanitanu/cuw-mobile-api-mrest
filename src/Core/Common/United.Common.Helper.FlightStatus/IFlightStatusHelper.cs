using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.FlightStatus;
using United.Service.Presentation.FlightResponseModel;
using United.Service.Presentation.SegmentModel;

namespace United.Common.Helper.FlightStatus
{
    public interface IFlightStatusHelper
    {
        Task<FlightStatusInfo> GetFlightStatusJson(int flightNumber, string flightDate, string origin);

        bool BypassGetCancelDelayReasonMessage(OperationalFlightSegment seg);

        Task<AirportAdvisoryMessage> GetAirportAdvisoryMessages(List<string> advisoryCheckAirports, string flightDate, string transactionId);

        string GetFormattedFlightTravelTime(TimeSpan scheduledFlightDuration);

        string GetAirportSegmentListParameter(List<string> advisoryCheckAirports);

        Task<FlightInformation> GetCSLFlightStatusJson(string flightNumber, string flightDate, string origin, string flifoAuthenticationToken, string sessionId);
    }
}