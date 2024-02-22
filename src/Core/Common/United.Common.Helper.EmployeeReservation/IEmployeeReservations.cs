using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Common.Helper.EmployeeReservation
{
    public interface IEmployeeReservations
    {
        MOBEmpTravelTypeAndJAProfileResponse GetTravelTypesAndJAProfile(MOBEmpTravelTypeAndJAProfileRequest request);
        Task<United.Service.Presentation.PersonModel.EmployeeTravelProfile> GetEmployeeProfile(int applicationId, string applicationVersion, string deviceId, string employeeId, string token, string sessionId);
        Task<EmployeeJA> GetEResEmp20PassriderDetails(string employeeId, string token, string TransactionId, int ApplicationId, string AppVersion, string DeviceId);
    }
}
