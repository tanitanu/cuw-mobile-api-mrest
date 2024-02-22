using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;

namespace United.Common.Helper.FSRHandler
{
    public class FSRNonstopSuggestFutureDate : IRule<MOBFSRAlertMessage>
    {
        private List<MOBSHOPFareWheelItem> _nonStopFareWheel;
        private string _origin;
        private string _destination;
        private DateTime _nonStopFutureDate = DateTime.MinValue;
        private DateTime _depatureDate;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private readonly IConfiguration _configuration;
        private const string dateFormat = "MM/dd/yyyy";
        private Lazy<DateTime> lastDateOfBookingWindow = null;
        private MOBMobileCMSContentMessages _messages;

        public FSRNonstopSuggestFutureDate(List<MOBSHOPFareWheelItem> nonStopFareWheel, string origin, string destination, string DepatdepartDate, MOBSHOPShopRequest restShopRequest, int tripIndex, IConfiguration configuration, MOBMobileCMSContentMessages messages = null)
        {
            _nonStopFareWheel = nonStopFareWheel;
            _origin = origin;
            _destination = destination;
            _depatureDate = Convert.ToDateTime(DepatdepartDate);
            _restShopRequest = restShopRequest.Clone();
            _tripIndex = tripIndex;
            _configuration = configuration;
            lastDateOfBookingWindow = new Lazy<DateTime>(() => DateTime.Now.Date.AddDays(_configuration.GetValue<int>("PS0B1BEmpAdvanceBookingDays")), LazyThreadSafetyMode.ExecutionAndPublication);
            _messages = messages;
        }

        public bool ShouldExecuteRule()
        {
            if (_nonStopFareWheel != null && _nonStopFareWheel.Count > 0)
            {
                // find date on the right of the fare wheel
                for (int _cal = _nonStopFareWheel.Count / 2; _cal < _nonStopFareWheel.Count; _cal++)
                {
                    if (_nonStopFareWheel[_cal].Value != null && _nonStopFareWheel[_cal].Value != "Not available")
                    {
                        _nonStopFutureDate = Convert.ToDateTime(_nonStopFareWheel[_cal].Key);
                        break;
                    }
                }

                // find date on the left of the fare wheel if couldn't find one on the right
                if (_nonStopFutureDate == DateTime.MinValue)
                {
                    for (int _cal = _nonStopFareWheel.Count / 2; _cal >= 0; _cal--)
                    {
                        if (_nonStopFareWheel[_cal].Value != null && _nonStopFareWheel[_cal].Value != "Not available")
                        {
                            _nonStopFutureDate = Convert.ToDateTime(_nonStopFareWheel[_cal].Key);
                            break;
                        }
                    }
                }
            }

            return (_nonStopFutureDate != DateTime.MinValue);
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed") && _messages != null) ?
                                _messages.HeadLine :
                                string.Format(_configuration.GetValue<string>("FSRNonstopSuggestFutureDateMsgHeader"), _origin, _destination, _nonStopFutureDate.ToString("dddd, MMMM dd"));
            bool roundTrip = _restShopRequest.SearchType.Equals("RT", StringComparison.OrdinalIgnoreCase);

            // Ensure dates are within booking window
            if (roundTrip)
            {
                int duration = (Convert.ToDateTime(_restShopRequest.Trips[1].DepartDate).Date - Convert.ToDateTime(_restShopRequest.Trips[0].DepartDate).Date).Days;
                var returnDate = _nonStopFutureDate.AddDays(duration);
                if (returnDate > lastDateOfBookingWindow.Value)
                    returnDate = lastDateOfBookingWindow.Value;

                _restShopRequest.Trips[0].DepartDate = _nonStopFutureDate.ToString(dateFormat);
                _restShopRequest.Trips[1].DepartDate = returnDate.ToString(dateFormat);
            }
            else // OW
            {
                _restShopRequest.Trips[0].DepartDate = _nonStopFutureDate.ToString(dateFormat);
            }

            var button = new MOBFSRAlertMessageButton();
            button.UpdatedShopRequest = _restShopRequest;
            button.UpdatedShopRequest.CameFromFSRHandler = true;
            button.UpdatedShopRequest.SessionId = null;
            button.UpdatedShopRequest.GetNonStopFlightsOnly = true;
            button.UpdatedShopRequest.GetFlightsWithStops = false;
            button.ButtonLabel = (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed") && _messages != null) ?
                                string.Format(_messages.ContentShort, _nonStopFutureDate.ToString("MMMM dd")) :
                                _configuration.GetValue<string>("FSRNonstopSuggestFutureDateButtonLabel");                       

            await Task.Delay(0);
            return new MOBFSRAlertMessage()
            {
                HeaderMessage = headerMsg,
                BodyMessage = (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed") && _messages != null) ?
                                string.Format(_messages.ContentFull, _origin, _destination, _nonStopFutureDate.ToString("dddd, MMMM dd")) :
                                null,
                MessageTypeDescription = FSRAlertMessageType.NonstopsSuggestion,
                MessageType = 0,
                Buttons = new List<MOBFSRAlertMessageButton> { button },
                RestHandlerType = MOBFSREnhancementType.SuggestNonStopFutureDate.ToString(),
                AlertType = (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed") && _messages != null) ?
                            MOBFSRAlertMessageType.Information.ToString() :
                            MOBFSRAlertMessageType.Warning.ToString(),
                IsAlertExpanded = (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed") && _messages != null) ? false : true
            };
        }
    }
}
