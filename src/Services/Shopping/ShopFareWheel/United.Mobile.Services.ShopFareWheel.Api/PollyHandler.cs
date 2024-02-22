using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace United.Mobile.Services.ShopFareWheel.Api
{
    public class PollyHandler
    {
        public static IAsyncPolicy<HttpResponseMessage> WaitAndRetry(int retryAttempt = 1, int sleepTimeInMillSecs = 0)
        {
            //Veracide does not like  => var jitterer = new Random();
            var retryWithJitterPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(retryAttempt,    // exponential back-off plus some jitter
                    retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(sleepTimeInMillSecs, retryAttempt))
                                    + TimeSpan.FromMilliseconds(50)
                );



            return retryWithJitterPolicy;
        }



        public static IAsyncPolicy<HttpResponseMessage> Timeout(double timeOut = 500) =>
             Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(timeOut));

    }
}
