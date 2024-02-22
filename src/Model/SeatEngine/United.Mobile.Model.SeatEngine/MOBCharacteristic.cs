using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.SeatMapEngine
{
    public class MOBCharacteristic
    {
        private string code;
        public string Code
        {
            get { return this.code; }
            set { this.code = value; }
        }

        private string value;
        public string Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
    }
}
