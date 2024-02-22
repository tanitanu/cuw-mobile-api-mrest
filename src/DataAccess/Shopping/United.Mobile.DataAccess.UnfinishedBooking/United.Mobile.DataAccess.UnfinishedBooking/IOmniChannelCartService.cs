using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnfinishedBooking
{
    public interface IOmniChannelCartService
    {
        Task<string> PurgeUnfinshedBookings(string token, string action, string sessionId);
        Task<string> GetFlightReservationData(string cartId, bool injectUrl,string token, string transactionId, string sessionId);
        Task<string> GetCartIdInformation(string cartId, string token, string transactionId, string sessionId);

    }
}
