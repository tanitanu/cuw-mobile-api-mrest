using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class GeoLocationInfo
    {
        private string placeId = string.Empty;
        private string locationId = string.Empty;
        private string venueId = string.Empty;
        private string latitude  = string.Empty;
        private string longitude  = string.Empty;
        private string lLPoiId = string.Empty;

        [JsonIgnore]
        public string PlaceId
        {
            get
            {
                return this.placeId;
            }
            set
            {
                this.placeId = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
                this.locationId = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
            }
        }

        public string LocationId
        {
            get
            {
                return this.placeId;
            }
            set
            {
                this.placeId = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
                this.locationId = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
            }
        }

        public string VenueId
        {
            get
            {
                return this.venueId;
            }
            set
            {
                this.venueId = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
            }
        }
        public string Latitude
        {
            get
            {
                return this.latitude;
            }
            set
            {
                this.latitude = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
            }
        }
        public string Longitude
        {
            get
            {
                return this.longitude;
            }
            set
            {
                this.longitude = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
            }
        }
        public string LLPoiId
        {
            get
            {
                return this.lLPoiId;
            }
            set
            {
                this.lLPoiId = (string.IsNullOrEmpty(value) || value.ToUpper().Equals("NULL")) ? string.Empty : value.Trim();
            }
        }
    }
}
