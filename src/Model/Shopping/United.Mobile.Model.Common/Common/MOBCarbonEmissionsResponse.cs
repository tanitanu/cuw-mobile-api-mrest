using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class MOBCarbonEmissionsResponse : MOBResponse
    {
        private string objectName = "United.Definition.MOBCarbonEmissionsResponse";
        public string ObjectName
        {
            get
            {
                return this.objectName;
            }
            set
            {
                this.objectName = value;
            }
        }

        private List<MOBCarbonEmissionData> carbonEmissionData;

        public List<MOBCarbonEmissionData> CarbonEmissionData
        {
            get { return carbonEmissionData; }
            set { carbonEmissionData = value; }
        }
    }
}
