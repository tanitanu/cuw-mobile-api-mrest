using System.Threading.Tasks;
using United.Mobile.Model.DeviceInitialization;

namespace United.Mobile.DataAccess.OnPremiseSQLSP
{
    public interface IDeviceInitializationService
    {
        Task<T> RegisterDevice<T>(string request, string key, string transId, string sessionId);
    }
}
