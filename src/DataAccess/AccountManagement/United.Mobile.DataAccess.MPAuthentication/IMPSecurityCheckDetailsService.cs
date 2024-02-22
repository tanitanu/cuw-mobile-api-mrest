using System.Threading.Tasks;

namespace United.Mobile.DataAccess.MPAuthentication
{
    public interface IMPSecurityCheckDetailsService
    {
        Task<string> GetMPSecurityCheckDetails(string token, string requestData, string sessionId);
        Task<string> UpdateTfaWrongAnswersFlag(string token, string requestData, string sessionId);       
    }
}
