namespace United.Mobile.Model.DeviceInitialization
{
    public class DeviceData
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Deviceid { get; set; } = string.Empty;
        public string PushToken { get; set; } = string.Empty;
        public int ApplicationId { get; set; }
    }
}
