using System.Collections.ObjectModel;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Service.Presentation.SegmentModel;

namespace United.Common.Helper.ManageRes
{
    public interface IManageReservation
    {
        Task<MOBPNRByRecordLocatorResponse> GetPNRByRecordLocatorCommonMethod(MOBPNRByRecordLocatorRequest request);

    }
}
