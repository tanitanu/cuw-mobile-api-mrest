using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.FlightReservation
{
    public interface IRequestReceiptByEmailService
    {
        Task<string> PostReceiptByEmailViaCSL(string token, string request, string sessionId, string path);
    }
}
