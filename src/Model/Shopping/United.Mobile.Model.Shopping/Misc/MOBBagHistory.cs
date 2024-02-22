using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class BagHistory
    {
        private string bagTagIssStn = string.Empty;
        private string bagTagIssDt = string.Empty;

        private BagFlight bagFlight;
        private BagStatus bagStatus;

        public BagHistory()
        {
        }

        public string BagTagIssStn
        {
            get
            {
                return this.bagTagIssStn;
            }
            set
            {
                this.bagTagIssStn = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string BagTagIssDt
        {
            get
            {
                return this.bagTagIssDt;
            }
            set
            {
                this.bagTagIssDt = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public BagFlight BagFlight
        {
            get
            {
                return this.bagFlight;
            }
            set
            {
                this.bagFlight = value;
            }
        }

        public BagStatus BagStatus
        {
            get
            {
                return this.bagStatus;
            }
            set
            {
                this.bagStatus = value;
            }
        }
    }
}
