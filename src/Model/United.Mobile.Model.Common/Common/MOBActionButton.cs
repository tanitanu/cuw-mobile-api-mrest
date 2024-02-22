using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Common.Common
{
    [Serializable()]
    public class MOBActionButton
    {
        private string actionURL;

        private string actionText;
        private int rank;
        private bool isPrimary;
        private bool isEnabled;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; }
        }

        public bool IsPrimary
        {
            get { return isPrimary; }
            set { isPrimary = value; }
        }

        public int Rank
        {
            get { return rank; }
            set { rank = value; }
        }

        public string ActionText
        {
            get { return actionText; }
            set { actionText = value; }
        }

        public string ActionURL
        {
            get { return actionURL; }
            set { actionURL = value; }
        }
    }
}
