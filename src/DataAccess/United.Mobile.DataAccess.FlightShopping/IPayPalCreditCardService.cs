using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.FlightShopping
{
    public interface IPayPalCreditCardService
    {
        Task<string> GetPayPalCreditCardResponse(string token, string requestData, string sessionId);
    }
}
