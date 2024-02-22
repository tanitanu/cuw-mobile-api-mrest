using System;
using System.Xml.Serialization;

namespace United.Mobile.Model.DeviceInitialization
{
    public class RegisterDevice
    {
        public string IdentifierForVendor { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string LocalizedModel { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string SystemVersion { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string ApplicationVersion { get; set; } = string.Empty;
    }
}
