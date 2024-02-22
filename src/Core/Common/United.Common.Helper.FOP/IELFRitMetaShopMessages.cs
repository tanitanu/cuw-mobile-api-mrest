using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Mobile.Model.Common;

namespace United.Common.Helper.FOP
{
    public interface IELFRitMetaShopMessages
    {
        Task<List<MOBItem>> GetELFShopMessagesForRestrictions(United.Mobile.Model.Shopping.MOBSHOPReservation reservation, int appId);
    }
}
