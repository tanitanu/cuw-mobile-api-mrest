using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping
{
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
}
