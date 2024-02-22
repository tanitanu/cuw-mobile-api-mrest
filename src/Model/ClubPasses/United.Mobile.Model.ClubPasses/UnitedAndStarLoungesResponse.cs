using System;
using System.Collections.Generic;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class UnitedAndStarLoungesResponse : MOBResponse
    {
        public string AirportCode { get; set; } = string.Empty;
        public List<UnitedStarLoungesLocations> UnitedStarLougesLocations { get; set; }
        public string AirportClubTitle { get; set; } = string.Empty; //"Locations at ORD"

        public UnitedAndStarLoungesResponse()
        {
            UnitedStarLougesLocations = new List<UnitedStarLoungesLocations>();
        }
    }
}
