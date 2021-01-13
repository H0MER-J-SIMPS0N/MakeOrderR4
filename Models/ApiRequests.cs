using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace MakeOrderR4v2.Models
{
    public static class ApiRequests
    {
        public static string Get(string address)
        {
            string result;
            GetHttpClient.Get().DefaultRequestHeaders.Clear();
            GetHttpClient.Get().DefaultRequestHeaders.TryAddWithoutValidation("Authorization", GetToken.Get().Value);
            using (var response = GetHttpClient.Get().GetAsync(address).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;                    
                }
                else
                {
                    throw new Exception(response.Content.ReadAsStringAsync().Result + "\n\n" + response.ToString());
                }
            }
            return result;
        }

        public static string Post(string address, HttpContent httpContent)
        {
            string result;
            GetHttpClient.Get().DefaultRequestHeaders.Clear();
            GetHttpClient.Get().DefaultRequestHeaders.TryAddWithoutValidation("Authorization", GetToken.Get().Value);
            using (HttpResponseMessage response = GetHttpClient.Get().PostAsync(address, httpContent).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    throw new Exception(response.Content.ReadAsStringAsync().Result + "\n\n" + response.ToString());
                }
            }
            return result;
        }
    }
}
