﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.UnitedClub
{
    public interface IPKDispenserPublicKeyService
    {
        Task<string> GetPKDispenserPublicKeyServices(string token, string sessionId);
    }
}
