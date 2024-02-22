using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class BagHistoryResponse :MOBResponse
    {
        public BagHistoryResponse()
            : base()
        {

        }

        private BagTagHistory bagTagHistory;
        public BagTagHistory BagTagHistory
        {
            get
            {
                return this.bagTagHistory;
            }
            set
            {
                this.bagTagHistory = value;
            }
        }
    }
}
