using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping
{

    [Serializable]
    public class RefreshCacheForSDLContentResponse : MOBResponse
    {
        public CSLContentMessagesResponse SDLContentMessageResponse { get; set; }
    }
}
