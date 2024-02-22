using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;

namespace United.Mobile.ScheduleChange.Domain
{
    public interface IRecordLocatorBusiness
    {
        Task<MOBPNRByRecordLocatorResponse> GetPNRByRecordLocator(MOBPNRByRecordLocatorRequest request);
    }
}
