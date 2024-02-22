namespace United.Mobile.Model.UnitedClubPasses
{
    public class PKDispenserPublicKeyCacheSessionResponse
    {
        public string MPNumber { get; set; }
        public string PkDispenserPublicKey { get; set; }
        public string SessionId { get; set; }
        public int Status { get; set; }
        public string Kid { get; set; }
        public string CryptoTypeID { get; set; }
        public PKDispenserPublicKeyCacheSessionResponse Data { get; set; }
    }
}
