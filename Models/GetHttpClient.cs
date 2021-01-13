using System;
using System.Net.Http;
using System.Threading;

namespace MakeOrderR4v2.Models
{
    class GetHttpClient
    {
        #region Fields and Properties
        private static HttpClient httpClient;
        private static readonly object syncRoot = new object();
        #endregion

        #region Methods
        public static HttpClient Get()
        {
            if (httpClient is null)
            {
                try
                {
                    Monitor.TryEnter(syncRoot, TimeSpan.FromSeconds(2));
                    if (httpClient is null)
                    {
                        httpClient = new HttpClient { Timeout = new TimeSpan(0, 3, 0) };
                    }
                }
                catch
                {
                    httpClient = null;
                }
                finally
                {
                    Monitor.Exit(syncRoot);
                }
            }
            return httpClient;
        }
        #endregion
    }
}
