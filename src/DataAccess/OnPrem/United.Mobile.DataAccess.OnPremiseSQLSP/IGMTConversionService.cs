using System.Threading.Tasks;

namespace United.Mobile.DataAccess.OnPremiseSQLSP
{
    public interface IGMTConversionService
    {
        Task<string> GETGMTTime(string localTime, string airportCode, string sessionId);
    }
}
