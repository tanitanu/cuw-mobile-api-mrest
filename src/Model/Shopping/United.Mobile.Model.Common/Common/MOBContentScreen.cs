using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Shopping
{
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
}
