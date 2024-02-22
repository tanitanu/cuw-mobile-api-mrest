using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnitedClub
{
    public interface IUnitedClubSQLDBService
    {
        Task<bool> VerifyMileagePlusWithDeviceAPPID(string deviceId, int applicationId, string mpNumber, string sessionId);
        Task<bool> ValidateAccountFromCache(string mpNumber, int appId, string appVersion, string deviceId, string hashPinCode, string sessionId);
    }
}
