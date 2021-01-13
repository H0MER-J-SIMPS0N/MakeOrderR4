using Avalonia.Threading;
using MakeOrderR4v2.Models;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace MakeOrderR4v2.ViewModels
{
    public class AssembleOrderViewModel : ViewModelBase
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static readonly string bundleToSendAddress = new Uri(new Uri(GetSettings.Get().BaseUrl), $"/r4/fhir/").ToString();
        public override string Name { get => "Собрать бандл"; }
        public Order Order { get; private set; }        

        private bool isWaiting;
        public bool IsWaiting
        {
            get => isWaiting;
            set => this.RaiseAndSetIfChanged(ref isWaiting, value);
        }
        public IObservable<bool> canExecuteCreateResultBundle { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CreateResultBundleCommand { get; set; }
        public IObservable<bool> canExecuteSendResultBundle { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SendResultBundleCommand { get; set; }
        #endregion

        #region .ctor
        public AssembleOrderViewModel(Order order)
        {
            Order = order;            
            canExecuteCreateResultBundle = this.WhenAnyValue(x => x.Order.PreanalyticsBundle, x => x.Order.PositionsBundle, (pr, pb) => !string.IsNullOrEmpty(pr) && !string.IsNullOrEmpty(pb));
            CreateResultBundleCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => CreateResultBundle()), canExecuteCreateResultBundle);
            canExecuteSendResultBundle = this.WhenAnyValue(x => x.Order.ResultBundle, (bts) => !string.IsNullOrEmpty(bts));
            SendResultBundleCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => SendResultBundle()), canExecuteSendResultBundle);
        } 

        #endregion

        #region Methods
        public void CreateResultBundle()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            logger.Info("Создаем бандл для отправки на сервис.");
            try
            {
                Dispatcher.UIThread.InvokeAsync(() => Order.CreateResultBundle());
                logger.Info($"Бандл для отправки на сервис создан:");
                Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"\r\n{Order.ResultBundle}"));
            } 
            catch (Exception ex)
            {
                logger.Error($"Бандл для отправки на сервис не создан по причине:\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => Order.ResultBundle = ex.ToString());
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        public void SendResultBundle()
        {
            string result = string.Empty;
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            logger.Info($"Начинаем отправку бандла на сервис.");
            try
            {
                Dispatcher.UIThread.InvokeAsync(() => result = ApiRequests.Post(bundleToSendAddress, new StringContent(Order.ResultBundle, Encoding.UTF8, "application/fhir+json")));
                Dispatcher.UIThread.InvokeAsync(() => Order.ResultBundle = result);
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Бандл отправлен на сервис. Получен ответ:\r\n{Order.ResultBundle}"));
            }
            catch (Exception ex)
            {
                logger.Error($"Бандл для отправки на сервис не отправлен по причине:\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => Order.ResultBundle = ex.ToString());
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        #endregion


    }
}
