using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class ClubMembership
    {
        public string MPNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MembershipTypeCode { get; set; } = string.Empty;
        public string MembershipTypeDescription { get; set; } = string.Empty;
        public string EffectiveDate { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public string CompanionMPNumber { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public byte[] BarCode { get; set; }
        public string BarCodeString { get; set; }
    }
}
