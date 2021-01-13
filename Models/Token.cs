using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace MakeOrderR4v2.Models
{
    public class Token
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Error { get; private set; }
        public string StatusCode { get; private set; }
        public string Value { get; set; }
        public string Duration { get; private set; }
        public DateTime BeginTime { get; private set; }
        public DateTime EndTime { get; private set; }
        #endregion

        #region .ctor
        public Token(Setting settings)
        {
            try
            {
                GetHttpClient.Get().DefaultRequestHeaders.Clear();
                FormUrlEncodedContent content = new FormUrlEncodedContent(settings.Data);
                using (var response = GetHttpClient.Get().PostAsync(settings.TokenAddress, content).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string answer = response.Content.ReadAsStringAsync().Result;
                        logger.Info("TOKEN: " + answer);
                        TokenResponse tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(answer);
                        Value = $"{tokenResponse.TokenType} {tokenResponse.AccessToken}";
                        BeginTime = DateTime.Now;
                        EndTime = BeginTime.AddMinutes(tokenResponse.ExpiresIn / 60 - 1);
                        Duration = BeginTime.ToString() + " - " + EndTime.ToString();
                        StatusCode = response.StatusCode.ToString();
                    }
                    else
                        throw new Exception(response.Content.ReadAsStringAsync().Result + "\n\n" + response.ToString());
                }
            }
            catch (Exception exc)
            {
                logger.Error("TOKENERROR: " + exc.ToString());
                Value = $"No token!!!";
                Error = exc.ToString();
                StatusCode = "exception";
            }
        }
        #endregion
    }
}
