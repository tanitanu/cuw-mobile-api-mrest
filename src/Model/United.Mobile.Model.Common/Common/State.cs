using System;
using System.Xml.Serialization;


namespace United.Mobile.Model.Common
{
    [Serializable()]
    [XmlRoot("MOBState")]
    public class State
    {
        private string code = string.Empty;
        private string name = string.Empty;

        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                this.code = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }
    }
}
