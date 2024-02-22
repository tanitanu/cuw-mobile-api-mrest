using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable]
    public class ClubDayPass
    {
        public string passCode { get; set; }  = string.Empty;
        public string mileagePlusNumber { get; set; } = string.Empty;
        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string clubPassCode { get; set; } = string.Empty;
        public double paymentAmount { get; set; }
        public string purchaseDate { get; set; } = string.Empty;
        public string expirationDate { get; set; } = string.Empty;
        public string expirationDateTime { get; set; } = string.Empty;
        public byte[] barCode { get; set; }
        //private EnElectronicClubPassesType electronicClubPassesType;
        public string electronicClubPassesType { get; set; }
    }

    [Serializable]
    public enum EnElectronicClubPassesType
    {
        PurchasedOTP = 0,
        ChaseCCOTP = 1,
        DefaultOTP = 2
    }
}
