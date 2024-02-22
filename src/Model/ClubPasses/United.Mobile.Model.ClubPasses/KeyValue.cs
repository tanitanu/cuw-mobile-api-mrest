using System;

namespace United.Mobile.Model.ClubPasses
{
    [Serializable()]
    public class KeyValue
    {
        private string key = string.Empty;
        private string value = string.Empty;
        public string Key
        {
            get
            {
                return this.key;
            }
            set
            {
                this.key = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public string Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
    }
}
