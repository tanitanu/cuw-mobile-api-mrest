using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class Channel
    {
        public string ChannelCode { get; set; } = string.Empty;
        public string ChannelDescription { get; set; } = string.Empty;
        public string ChannelTypeCode { get; set; } = string.Empty;
        public string ChannelTypeDescription { get; set; } = string.Empty;
    }
}
