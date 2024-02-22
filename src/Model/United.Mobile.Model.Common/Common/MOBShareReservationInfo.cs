using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Common.Common
{
    [Serializable]
    public class MOBShareReservationInfo
    {
        public string displayHeader;
        public string displayBody;
        public string displayLink;
        public List<MOBHtmlItem> displayOption;

        public string DisplayHeader { get { return this.displayHeader; } set { this.displayHeader = value; } }
        public string DisplayBody { get { return this.displayBody; } set { this.displayBody = value; } }
        public List<MOBHtmlItem> DisplayOption { get { return this.displayOption; } set { this.displayOption = value; } }
        public string DisplayLink { get { return this.displayLink; } set { this.displayLink = value; } }
    }
}
