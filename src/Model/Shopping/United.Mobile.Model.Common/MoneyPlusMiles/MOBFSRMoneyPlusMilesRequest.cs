using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping.Common.MoneyPlusMiles
{
    public class MOBFSRMoneyPlusMilesRequest : MOBRequest
    {
        private string sessionId;

        public string SessionId
        {
            get { return sessionId; }
            set { sessionId = value; }
        }


        private string cartId;

        public string CartId
        {
            get { return cartId; }
            set { cartId = value; }
        }

        private string mileagePlusNumer;

        public string MileagePlusNumer
        {
            get { return mileagePlusNumer; }
            set { mileagePlusNumer = value; }
        }
    }
}
