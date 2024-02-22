using System.Threading.Tasks;

namespace United.Mobile.DataAccess.Common
{
    public interface ISSOTokenKeyService
    {
        Task<string> DecryptToken(string token, string sessionId, string encryptedData);
        Task<string> EncryptKey(string token, string sessionId, string Key);
        Task<string> GetPartnerSSOToken(string token, string sessionId, string mpNumber);
    }
}
