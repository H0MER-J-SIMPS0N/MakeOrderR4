using Avalonia.Threading;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MakeOrderR4v2.Models;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace MakeOrderR4v2.ViewModels
{
    public class PreanalyticsViewModel : ViewModelBase
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public override string Name { get => "Преаналитика"; }
        public static readonly string preanalyticsRequestAddress = new Uri(new Uri(GetSettings.Get().BaseUrl), $"/r4/fhir/$x-preanalytics/").ToString();
        private bool isSelected;
        public override bool IsSelected
        {
            get => isSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref isSelected, value);
            }
        }

        private bool isWaiting;
        public bool IsWaiting
        {
            get => isWaiting;
            set
            {
                this.RaiseAndSetIfChanged(ref isWaiting, value);
            }
        }
        private Order _order;
        public Order Order
        {
            get => _order;
            set => this.RaiseAndSetIfChanged(ref _order, value);
        }
        public IObservable<bool> canExecuteGetPreanalytics { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> GetPreanalyticsCommand { get; set; }
        #endregion

        #region .ctor
        public PreanalyticsViewModel(Order order)
        {
            Order = order;
            canExecuteGetPreanalytics = this.WhenAnyValue(x => x.Order.PreanalyticsRequestBundle, (bfp) => bfp?.Length > 100);
            GetPreanalyticsCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => GetPreanalytics()), canExecuteGetPreanalytics);
        }
        #endregion

        #region Methods
        private void GetPreanalytics()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            logger.Info("Получаем преаналитику!");
            try
            {
                string res = ApiRequests.Post(preanalyticsRequestAddress, new StringContent(Order.PreanalyticsRequestBundle, Encoding.UTF8, "application/fhir+json"));
                try
                {
                    var bundle = new FhirJsonSerializer(new SerializerSettings() { Pretty = true }).SerializeToString(new FhirJsonParser().Parse<Bundle>(res));
                    Dispatcher.UIThread.InvokeAsync(() => Order.PreanalyticsBundle = bundle);
                    Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Преаналитика получена"));
                    Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"\r\n{Order.PreanalyticsBundle}"));
                }
                catch (Exception ex)
                {
                    logger.Error($"NomenclatureBundle: Не удалось получить преаналитику!!!\r\n{ex}");
                    Dispatcher.UIThread.InvokeAsync(() => Order.PreanalyticsBundle = $"NomenclatureBundle: Не удалось получить преаналитику!!!\r\n{ex}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"NomenclatureBundle: Не удалось получить преаналитику!!!\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => Order.PreanalyticsBundle = $"NomenclatureBundle: Не удалось получить преаналитику!!!\r\n{ex}");
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        #endregion

    }
}
