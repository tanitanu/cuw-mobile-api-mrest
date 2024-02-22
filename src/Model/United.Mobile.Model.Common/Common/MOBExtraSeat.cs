using System;

namespace United.Mobile.Model.Common.Common
{
    [Serializable()]
    public class MOBExtraSeat
    {
        private int selectedPaxId;

        public int SelectedPaxId
        {
            get { return selectedPaxId; }
            set { selectedPaxId = value; }
        }


        private string purpose;

        public string Purpose
        {
            get { return purpose; }
            set { purpose = value; }
        }


    }
}
