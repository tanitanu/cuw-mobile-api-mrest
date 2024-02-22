using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Shopping.Common.MoneyPlusMiles
{
    public class MOBFSRMoneyPlusMilesResponse :MOBResponse
    {
        private bool isEligibleForMoneyPlusMiles;

        public bool IsEligibleForMoneyPlusMiles
        {
            get { return isEligibleForMoneyPlusMiles; }
            set { isEligibleForMoneyPlusMiles = value; }
        }

        private List<MOBActionButton> fsrMoneyPlusMilesActions;

        public List<MOBActionButton> FsrMoneyPlusMilesActions
        {
            get { return fsrMoneyPlusMilesActions; }
            set { fsrMoneyPlusMilesActions = value; }
        }
    }
}
