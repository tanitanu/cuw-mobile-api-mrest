using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.Shopping
{
    public interface IFlightShoppingBaseClient
    {
        public Task<T> PostAsync<T>(string token, string sessionId, string action, string request);

    }
}
