using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;

namespace United.Mobile.ScheduleChange.Domain
{
    public interface IRecordLocatorBusiness
    {
        MOBPNRByRecordLocatorResponse GetPNRByRecordLocator(MOBPNRByRecordLocatorRequest request);
    }
}
