using Avalonia.Threading;
using DynamicData;
using MakeOrderR4v2.Models;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MakeOrderR4v2.ViewModels
{
    public class SelectSamplesViewModel : ViewModelBase
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public override string Name { get => "Выбор образцов"; }
        private Order order;
        public Order Order
        {
            get => order;
            set => this.RaiseAndSetIfChanged(ref order, value);
        }

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
            set => this.RaiseAndSetIfChanged(ref isWaiting, value);
        }
        ObservableCollection<KeyValuePair<string, NomenclaturePosition>> ExactlyOneSpecimensNomenclature { get; set; } = new ObservableCollection<KeyValuePair<string, NomenclaturePosition>>();

        ObservableCollection<KeyValuePair<string, NomenclaturePosition>> ManySpecimensNomenclature { get; set; } = new ObservableCollection<KeyValuePair<string, NomenclaturePosition>>();
        public IObservable<bool> canExecuteSelectSpecimens { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SelectSpecimensCommand { get; set; }
        public IObservable<bool> canExecuteCreatePreanalyticsBundle { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CreatePreanalyticsBundleCommand { get; set; }

        #endregion

        #region .ctor
        public SelectSamplesViewModel(Order order)
        {
            Order = order;
            canExecuteSelectSpecimens = this.WhenAnyValue(x => x.Order.PositionsBundle, (ob) => ob?.Length > 100);
            SelectSpecimensCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => SelectSpecimens()), canExecuteSelectSpecimens);
            canExecuteCreatePreanalyticsBundle = this.WhenAnyValue(x => x.Order.PositionsBundle, (opb) => !string.IsNullOrWhiteSpace(opb));
            CreatePreanalyticsBundleCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => CreatePreanalyticsRequestBundle()), canExecuteCreatePreanalyticsBundle);
        }

        #endregion

        #region Methods
        public void SelectSpecimens()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            try
            {
                var temp = Order.Positions.Where(x => x.Value.IsExactlyOneSpecimen && !x.Value.IsOnlyRequiredSpecimens);
                Dispatcher.UIThread.InvokeAsync(() => ExactlyOneSpecimensNomenclature.Clear());
                Dispatcher.UIThread.InvokeAsync(() => ExactlyOneSpecimensNomenclature.AddRange(temp.ToList()));
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Количество позиций в заказе с возможностью выбора только одного образца из списка - {ExactlyOneSpecimensNomenclature?.Count}"));
                var temp2 = Order.Positions.Where(x => !x.Value.IsExactlyOneSpecimen && !x.Value.IsOnlyRequiredSpecimens);
                Dispatcher.UIThread.InvokeAsync(() => ManySpecimensNomenclature.Clear());
                Dispatcher.UIThread.InvokeAsync(() => ManySpecimensNomenclature.AddRange(temp2.ToList()));
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Количество позиций в заказе с возможностью выбора нескольких образцов из списка - {ManySpecimensNomenclature.Count}"));
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(() => ExactlyOneSpecimensNomenclature.Clear());
                Dispatcher.UIThread.InvokeAsync(() => ManySpecimensNomenclature.Clear());
                logger.Error($"Не удалось разделить образцы на ExactlyOne и Many по причине:\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => Order.PositionsBundle = $"Не удалось разделить образцы на ExactlyOne и Many по причине:\r\n{ex}");
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        public void CreatePreanalyticsRequestBundle()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            try
            {
                Dispatcher.UIThread.InvokeAsync(() => Order.СreatePreanalyticsRequestBundleText());
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Bundle для запроса преаналитики создан"));
                Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"\r\n{Order?.PreanalyticsRequestBundle ?? string.Empty}"));
            }
            catch (Exception ex)
            {
                logger.Error($"Не удалось создать Bundle для преаналитики по причине:\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => Order.PositionsBundle = $"Не удалось создать Bundle для преаналитики по причине:\r\n{ex}");
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        #endregion

    }
}
