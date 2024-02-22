using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPFareFamily
    {
        public string FareFamily { get; set; } = string.Empty;
        public int MaxMileage { get; set; } = -1;
        public string MaxPrice { get; set; } = string.Empty;
        public int MinMileage { get; set; } = -1;
        public string MinPrice { get; set; } = string.Empty;
        public bool MinPriceInSummary { get; set; } = false;

    }
}
