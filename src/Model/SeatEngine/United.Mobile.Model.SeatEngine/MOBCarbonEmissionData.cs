using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.SeatEngine
{
    [Serializable]
    public class MOBCarbonEmissionData
    {
        private string flightHash;

        public string FlightHash
        {
            get
            {
                return this.flightHash;
            }
            set
            {
                this.flightHash = value;
            }
        }

        private MOBItemWithIconName carbonDetails;

        public MOBItemWithIconName CarbonDetails
        {
            get { return carbonDetails; }
            set { carbonDetails = value; }
        }

        private MOBContentScreen contentScreen;

        public MOBContentScreen ContentScreen
        {
            get { return contentScreen; }
            set { contentScreen = value; }
        }
    }

    [Serializable]
    public class MOBItemWithIconName
    {
        private string optionDescription = string.Empty;
        private string optionIcon = string.Empty;

        public string OptionDescription
        {
            get
            {
                return this.optionDescription;
            }
            set
            {
                this.optionDescription = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string OptionIcon
        {
            get
            {
                return this.optionIcon;
            }
            set
            {
                this.optionIcon = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
    }

    [Serializable]
    public class MOBContentScreen
    {
        private string pageTitle;
        private string header;
        private string body;
        private List<MOBContentDetails> contentDetails;
        private string footerMessage;
        private List<MOBActionButton> buttons;
        private bool isHtmlBodyText;

        public string PageTitle
        {
            get
            {
                return pageTitle;
            }
            set
            {
                pageTitle = value;
            }
        }

        public string Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
            set
            {
                body = value;
            }
        }

        public List<MOBContentDetails> ContentDetails
        {
            get
            {
                return contentDetails;
            }
            set
            {
                contentDetails = value;
            }
        }

        public string FooterMessage
        {
            get
            {
                return footerMessage;
            }
            set
            {
                footerMessage = value;
            }
        }

        public List<MOBActionButton> Buttons
        {
            get
            {
                return buttons;
            }
            set
            {
                buttons = value;
            }
        }

        public bool IsHtmlBodyText
        {
            get
            {
                return isHtmlBodyText;
            }
            set
            {
                isHtmlBodyText = value;
            }
        }
    }
    [Serializable]
    public class MOBContentDetails
    {
        private MOBItemWithIconName iconData;

        public MOBItemWithIconName IconData
        {
            get { return iconData; }
            set { iconData = value; }
        }

        private List<MOBItem> subContent;

        public List<MOBItem> SubContent
        {
            get { return subContent; }
            set { subContent = value; }
        }
    }

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


    [Serializable()]
    public class MOBItem
    {
        private string id = string.Empty;
        private string currentValue = string.Empty;
        private bool saveToPersist = false;

        public string Id
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string CurrentValue
        {
            get
            {
                return this.currentValue;
            }
            set
            {
                this.currentValue = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public bool SaveToPersist
        {
            get
            {
                return this.saveToPersist;
            }
            set
            {
                this.saveToPersist = value;
            }
        }
    }
}
