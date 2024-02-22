using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable]
    public class Name
    {
        public string Title { get; set; } = string.Empty;
        public string First { get; set; } = string.Empty;
        public string Middle { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
    }
}
