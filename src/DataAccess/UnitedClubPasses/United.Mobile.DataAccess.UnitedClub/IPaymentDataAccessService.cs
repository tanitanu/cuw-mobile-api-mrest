﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnitedClub
{
    public interface IPaymentDataAccessService
    {
        Task<string> AddPaymentNew(string token, string requestData, string sessionId);
    }
}
