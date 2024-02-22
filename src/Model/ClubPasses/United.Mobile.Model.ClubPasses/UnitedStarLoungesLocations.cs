using System;
using System.Collections.Generic;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class UnitedStarLoungesLocations
    {
        public string ClubType { get; set; } = string.Empty;// "UnitedLounge","STARLOUNGE"
        public List<Club> Clubs { get; set; }
        public string ClubTypeTitle { get; set; } = string.Empty; // "United Club and other United Lounges" / "Star Allicance-affiliated lounges" For testing purpose try to send a register trade mark to check how the client render the trade mark at iOS & Andriod
        public UnitedStarLoungesLocations()
        {
            Clubs = new List<Club>();
        }
    }
}
