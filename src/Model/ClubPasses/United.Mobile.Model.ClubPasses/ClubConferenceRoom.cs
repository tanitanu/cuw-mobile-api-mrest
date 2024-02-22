using System;
using System.Collections.Generic;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class ClubConferenceRoom
    {
        public string Description { get; set; } = string.Empty;
        public string ReservationPhoneNumber { get; set; } = string.Empty;
        public List<string> BusinessAmenities { get; set; } = new List<string>();
        public ClubConferenceRoom()
        {
            BusinessAmenities = new List<string>();
        }
    }
}
