using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;

namespace United.Common.Helper.FSRHandler
{
    public abstract class FSRSeasonalSuggestFutureDateBase : IRule<MOBFSRAlertMessage>
    {
        #region Variables

        protected ShopResponse _cslShopResponse;
        protected MOBSHOPShopRequest _restShopRequest;
        protected int _tripIndex = 0;
        protected const string DepartureFutureTravelDateErrorMinorCode = "10052"; //  minor desc : ServiceErrorDepartureFutureTravelDate
        protected const string ArrivalFutureTravelDateErrorMinorCode = "10053"; // minor desc : ServiceErrorArrivalFutureTravelDate
        protected const string DateToStringFormat = "MM/dd/yyyy";
        private DateTime futureTravelDate;
        private Lazy<DateTime> lastDateOfBookingWindow;
        private Lazy<DateTime> firstDateOfBookingWindow = new Lazy<DateTime>(() => DateTime.Now.Date, LazyThreadSafetyMode.ExecutionAndPublication);
        private ShopResponse cslShopResponse;
        private MOBSHOPShopRequest restShopRequest;
        private readonly IConfiguration _configuration;
        #endregion

        #region Constructor

        public FSRSeasonalSuggestFutureDateBase(ShopResponse cslShopResponse, MOBSHOPShopRequest restShopRequest, IConfiguration configuration)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _tripIndex = cslShopResponse.LastTripIndexRequested - 1;
            _configuration = configuration;
            lastDateOfBookingWindow = new Lazy<DateTime>(() => DateTime.Now.Date.AddDays(Convert.ToInt32(_configuration.GetValue<string>("PS0B1BEmpAdvanceBookingDays"))), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected FSRSeasonalSuggestFutureDateBase(ShopResponse cslShopResponse, MOBSHOPShopRequest restShopRequest)
        {
            this.cslShopResponse = cslShopResponse;
            this.restShopRequest = restShopRequest;
        }

        #endregion

        #region Properties

        protected DateTime LastDateOfBookingWindow { get { return lastDateOfBookingWindow.Value; } }
        protected DateTime FirstDateOfBookingWindow { get { return firstDateOfBookingWindow.Value; } }
        protected DateTime FutureTravelDate { get { return futureTravelDate.Date; } }

        #endregion

        #region Abstract Functions

        protected abstract string GetSeasonalSuggestFutureDateBodyMessage();
        protected abstract MOBSHOPShopRequest GetSearchNearByButtonUpdatedRequest(MOBSHOPShopRequest restShopRequest);
        protected abstract Tuple<DateTime, DateTime> GetNewTripDurationWindowForRT(MOBSHOPShopRequest restShopRequest);
        protected abstract MOBFSREnhancementType GetFRSEnhancementType();

        #endregion

        #region Interface Implementation

        public virtual bool ShouldExecuteRule()
        {
            return (_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                     && (_cslShopResponse.Trips[_tripIndex].Flights == null || !_cslShopResponse.Trips[_tripIndex].Flights.Any()) // no results
                     && !string.IsNullOrWhiteSpace(_cslShopResponse.Trips[_tripIndex].FutureTravelDate) && DateTime.TryParse(_cslShopResponse.Trips[_tripIndex].FutureTravelDate, out futureTravelDate));
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            await Task.Delay(0);
            return new MOBFSRAlertMessage()
            {
                HeaderMessage = _configuration.GetValue<string>("FSRNoResultsMsgHeader"),
                BodyMessage = _configuration.GetValue<string>("FSRNoResultsSuggestNearbyMsgBody") + "\r\n" + GetSeasonalSuggestFutureDateBodyMessage(),
                MessageTypeDescription = FSRAlertMessageType.None,
                MessageType = 0,
                Buttons = new List<MOBFSRAlertMessageButton> { GetSearchNearByButton(_restShopRequest.Clone()), GetSeasonalButton(_restShopRequest.Clone()) },
                RestHandlerType = GetFRSEnhancementType().ToString(),
            };
        }

        #endregion

        #region Other Functions

        protected Tuple<DateTime, DateTime> CalculateNewTripDurationWindowForRT(Tuple<DateTime, DateTime> departAndFutureDatePair, Tuple<DateTime, DateTime> returnAndFutureDatePair)
        {
            if (departAndFutureDatePair.Item1 == DateTime.MinValue || returnAndFutureDatePair.Item1 == DateTime.MinValue)
                throw new ArgumentException("Invalid trip duration window.");

            if (departAndFutureDatePair.Item2 == DateTime.MinValue && returnAndFutureDatePair.Item2 == DateTime.MinValue)
                throw new ArgumentException("Invalid future travel date.");

            int duration = (returnAndFutureDatePair.Item1.Date - departAndFutureDatePair.Item1.Date).Days;

            DateTime newDepartureDate, newReturnDate;

            if (departAndFutureDatePair.Item2 != DateTime.MinValue)
            {
                newDepartureDate = departAndFutureDatePair.Item2;
                newReturnDate = newDepartureDate.AddDays(duration);
                newReturnDate = newReturnDate > LastDateOfBookingWindow ? LastDateOfBookingWindow : newReturnDate;

                return Tuple.Create(newDepartureDate, newReturnDate);
            }

            newReturnDate = returnAndFutureDatePair.Item2;
            newDepartureDate = newReturnDate.AddDays(-duration);
            newDepartureDate = newDepartureDate < FirstDateOfBookingWindow ? FirstDateOfBookingWindow : newDepartureDate;

            return Tuple.Create(newDepartureDate, newReturnDate);
        }

        private DateTime CalculateDepartureDateForOW()
        {
            return FutureTravelDate > LastDateOfBookingWindow ? LastDateOfBookingWindow : FutureTravelDate;
        }

        private MOBFSRAlertMessageButton GetSeasonalButton(MOBSHOPShopRequest restShopRequest)
        {
            bool roundTrip = restShopRequest.SearchType.Equals("RT", StringComparison.OrdinalIgnoreCase);

            if (roundTrip)
            {
                var departureDate = Convert.ToDateTime(restShopRequest.Trips[0].DepartDate).Date;
                var returnDate = Convert.ToDateTime(restShopRequest.Trips[1].DepartDate).Date;
                var newDepartureAndReturnDate = GetNewTripDurationWindowForRT(restShopRequest);

                restShopRequest.Trips[0].DepartDate = newDepartureAndReturnDate.Item1.ToString(DateToStringFormat);
                restShopRequest.Trips[1].DepartDate = newDepartureAndReturnDate.Item2.ToString(DateToStringFormat);
            }
            else // OW
            {
                restShopRequest.Trips[0].DepartDate = CalculateDepartureDateForOW().ToString(DateToStringFormat);
            }

            restShopRequest.SessionId = null;
            restShopRequest.GetNonStopFlightsOnly = true;
            restShopRequest.GetFlightsWithStops = false;

            return new MOBFSRAlertMessageButton { ButtonLabel = _configuration.GetValue<string>("FSRNoResultsSearchFutureDateButtonLabel"), UpdatedShopRequest = restShopRequest };
        }

        private MOBFSRAlertMessageButton GetSearchNearByButton(MOBSHOPShopRequest restShopRequest)
        {
            return new MOBFSRAlertMessageButton
            {
                UpdatedShopRequest = GetSearchNearByButtonUpdatedRequest(restShopRequest),
                ButtonLabel = _configuration.GetValue<string>("FSRSearchNearbyButtonLabel")
            };
        }
        #endregion
    }
}
