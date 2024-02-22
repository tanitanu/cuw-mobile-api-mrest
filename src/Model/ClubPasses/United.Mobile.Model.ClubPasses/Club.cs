using System;
using System.Collections.Generic;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class Club
    {
        private string id { get; set; } = string.Empty;

        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        private string name { get; set; } = string.Empty;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string Location { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Web { get; set; } = string.Empty;

        private string notes = string.Empty;

        public string Notes
        {
            get
            {
                return this.notes;
            }
            set
            {
                this.notes = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public bool BFC { get; set; }
        public List<string> Hours { get; set; } = new List<string>();
        public List<string> PhoneNumbers { get; set; } = new List<string>();
        public List<ClubAmenity> Amenities { get; set; } = new List<ClubAmenity>();
        public List<ClubEligibility> Eligibilities { get; set; } = new List<ClubEligibility>();
        public GeoLocationInfo GeoLocation { get; set; } = new GeoLocationInfo();
        public ClubConferenceRoom ConferenceRoom { get; set; } = new ClubConferenceRoom();

        private string clubType = string.Empty;

        public string ClubType
        {
            get
            {
                return this.clubType;
            }
            set
            {
                this.clubType = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }


        public bool PClub { get; set; } = false;
        public bool RedCarpetClub { get; set; } = false;
        public string SortField { get; set; } = string.Empty;
        public string ClubTitle { get; set; } = string.Empty; // "Lounge Details" / "United Polaris Lounge" 
        public bool AllowOTPPurchase { get; set; } = false;
        public bool ShowAirportMap { get; set; } = false;
        public List<KeyValue> DayAndHours { get; set; } = new List<KeyValue>();
        public string EligibilityDescription { get; set; } = string.Empty;
        public string MemberAirlineCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string MemberAirlineName { get; set; } = string.Empty;
        public string NoHourOperationText { get; set; } = string.Empty;

        public Club()
        {
            Hours = new List<string>();
            PhoneNumbers = new List<string>();
            Amenities = new List<ClubAmenity>();
            Eligibilities = new List<ClubEligibility>();
            DayAndHours = new List<KeyValue>();
        }
    }
}
