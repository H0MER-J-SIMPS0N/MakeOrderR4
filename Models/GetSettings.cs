using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace MakeOrderR4v2.Models
{
    class GetSettings
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly object syncRoot = new object();
        private static Setting settings;
        #endregion

        #region Methods
        public static Setting Get()
        {
            if (settings is null)
            {
                string settingsText = string.Empty;
                try
                {
                    Monitor.TryEnter(syncRoot, TimeSpan.FromSeconds(2));
                    settingsText = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
                }
                catch (Exception ex)
                {
                    logger.Error($"Не удалось прочитать файл настроек {Path.Combine(Directory.GetCurrentDirectory(), "settings.json")} по причине:\r\n" + ex.ToString());
                }
                finally
                {
                    Monitor.Exit(syncRoot);
                }
                if (!string.IsNullOrEmpty(settingsText))
                {
                    try
                    {
                        Monitor.TryEnter(syncRoot, TimeSpan.FromSeconds(2));
                        settings = JsonConvert.DeserializeObject<Setting>(settingsText);
                        logger.Info($"BaseUrl - {settings.BaseUrl}; TokenAddress - {settings.TokenAddress}");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Не удалось десериализовать данные из файла настроек{Path.Combine(Directory.GetCurrentDirectory(), "settings.json")} по причине:\r\n" + ex.ToString());
                    }
                    finally
                    {
                        Monitor.Exit(syncRoot);
                    }
                }
            }
            return settings;
        }
        #endregion
    }
}
