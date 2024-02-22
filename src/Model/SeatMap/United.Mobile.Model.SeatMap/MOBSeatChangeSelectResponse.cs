using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping.Booking;
using MOBSeat = United.Mobile.Model.Shopping.Misc.Seat;


namespace United.Mobile.Model.SeatMap
{
    [Serializable()]
    public class MOBSeatChangeSelectResponse : MOBResponse
    {
        public string InterlineErrorMessage { get; set; } 

        public MOBSeatChangeSelectRequest Request { get; set; }

        public string SessionId { get; set; } = string.Empty;

        public List<MOBSeat> Seats { get; set; }

        public List<Model.Shopping.MOBSeatMap> SeatMap { get; set; }

        public List<MOBTypeOption> ExitAdvisory { get; set; }

        public List<MOBBKTraveler> BookingTravelerInfo
        {
            get { return this.bookingTravlerInfo; }
            set { this.bookingTravlerInfo = value; }
        }

        private List<MOBBKTraveler> bookingTravlerInfo;
        public List<MOBBKTraveler> BookingTravlerInfo
        {
            get { return this.bookingTravlerInfo; }
            set { this.bookingTravlerInfo = value; }
        }

        public List<Model.Shopping.Booking.MOBBKTrip> SelectedTrips { get; set; }

        private InterLineDeepLink interLineDeepLink;

        public InterLineDeepLink InterLineDeepLink
        {
            get { return this.interLineDeepLink; }
            set { this.interLineDeepLink = value; }
        }
    }

    [Serializable()]
    public class InterLineDeepLink
    {
        private bool showInterlineAdvisoryMessage;
        private string interlineAdvisoryMessage;
        private string interlineAdvisoryTitle;
        private string interlineAdvisoryAlertTitle;

        public string InterlineAdvisoryAlertTitle
        {
            get { return interlineAdvisoryAlertTitle; }
            set { interlineAdvisoryAlertTitle = value; }
        }
        public bool ShowInterlineAdvisoryMessage
        {
            get { return showInterlineAdvisoryMessage; }
            set { showInterlineAdvisoryMessage = value; }
        }
        public string InterlineAdvisoryMessage
        {
            get { return interlineAdvisoryMessage; }
            set { interlineAdvisoryMessage = value; }
        }
        public string InterlineAdvisoryTitle
        {
            get { return interlineAdvisoryTitle; }
            set { interlineAdvisoryTitle = value; }
        }

        private string interlineAdvisoryDeepLinkURL;
        public string InterlineAdvisoryDeepLinkURL
        {
            get { return interlineAdvisoryDeepLinkURL; }
            set { interlineAdvisoryDeepLinkURL = value; }
        }
    }
}

