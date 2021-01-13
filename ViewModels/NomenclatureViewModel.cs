using Avalonia.Threading;
using DynamicData;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MakeOrderR4v2.Models;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeOrderR4v2.ViewModels
{
    class NomenclatureViewModel : ViewModelBase
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly FhirJsonParser parser = new FhirJsonParser();
        public override string Name { get => "Номенклатура"; }
        private bool isSelected;
        public override bool IsSelected
        {
            get => isSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref isSelected, value);
            }
        }
        private ProcessedBundle ProcessedBundle { get; set; }
        private string contract;
        public string Contract
        {
            get => contract;
            set => this.RaiseAndSetIfChanged(ref contract, value);
        }
        public string CatalogRequestAddress
        {
            get => new Uri(new Uri(GetSettings.Get().BaseUrl), $"r4/fhir/catalog/{Contract}").ToString();
        }

        private bool isWaiting;
        public bool IsWaiting
        {
            get => isWaiting;
            set => this.RaiseAndSetIfChanged(ref isWaiting, value);
        }

        public Bundle NomenclatureBundle { get; private set; }
        public Order Order { get; set; }

        private string textToFind;
        public string TextToFind
        {
            get => textToFind;
            set => this.RaiseAndSetIfChanged(ref textToFind, value);
        }
        ObservableCollection<NomenclaturePosition> FoundNomenclaturePositions { get; set; } = new ObservableCollection<NomenclaturePosition>();
        private NomenclaturePosition selectedFoundPosition;
        public NomenclaturePosition SelectedFoundPosition
        {
            get => selectedFoundPosition;
            set => this.RaiseAndSetIfChanged(ref selectedFoundPosition, value);
        }
        private KeyValuePair<string, NomenclaturePosition> selectedAddedPosition = new KeyValuePair<string, NomenclaturePosition>();
        public KeyValuePair<string, NomenclaturePosition> SelectedAddedPosition
        {
            get => selectedAddedPosition;
            set => this.RaiseAndSetIfChanged(ref selectedAddedPosition, value);
        }
        public IObservable<bool> canExecuteGetNomenclatureBundle { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> GetNomenclatureBundleCommand { get; set; }
        public IObservable<bool> canExecuteFindNomenclature { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> FindNomenclatureCommand { get; set; }
        public IObservable<bool> canExecuteAddPosition { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AddPositionCommand { get; set; }
        public IObservable<bool> canExecuteRemovePosition { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RemovePositionCommand { get; set; }
        public IObservable<bool> canExecuteRemoveAllPositions { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RemoveAllPositionsCommand { get; set; }
        public IObservable<bool> canExecuteMakeBundleOfPositions { get; set; }
        public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> MakeBundleOfPositionsCommand { get; set; }

        #endregion

        #region .ctor
        public NomenclatureViewModel(Order order)
        {
            Order = order;
            Contract = "C000003409";
            canExecuteGetNomenclatureBundle = this.WhenAnyValue(x => x.Contract, (c) => !string.IsNullOrWhiteSpace(c) && Validate.Contract(c));
            GetNomenclatureBundleCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => GetNomenclatureBundle()), canExecuteGetNomenclatureBundle);
            canExecuteFindNomenclature = this.WhenAnyValue(x => x.TextToFind, (t) => !string.IsNullOrEmpty(t) && Validate.Text(t));
            FindNomenclatureCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => FindNomenclature()), canExecuteFindNomenclature);
            canExecuteAddPosition = this.WhenAnyValue(x => x.SelectedFoundPosition, x => x.Order.Positions, (sfp, ap) =>
                        sfp != null
                        && Validate.AddNomenclature(sfp, ap));
            AddPositionCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => AddPosition()), canExecuteAddPosition);
            canExecuteRemovePosition = this.WhenAnyValue(x => x.Order.Positions, x => x.SelectedAddedPosition, (ap, sap) => ap != null && ap.Count > 0 && !string.IsNullOrEmpty(sap.Key));
            RemovePositionCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => RemovePosition()), canExecuteRemovePosition);
            canExecuteRemoveAllPositions = this.WhenAnyValue(x => x.Order.Positions, (ap) => ap != null && ap.Count > 0);
            RemoveAllPositionsCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => RemoveAllPositions()), canExecuteRemoveAllPositions);
            canExecuteMakeBundleOfPositions = this.WhenAnyValue(x => x.Order.Positions, (ap) => ap != null && ap.Count > 0);
            MakeBundleOfPositionsCommand = ReactiveCommand.CreateFromTask(async () => await System.Threading.Tasks.Task.Factory.StartNew(() => GetPositionsBundle()), canExecuteMakeBundleOfPositions);
        }
        #endregion

        #region Methods
        private void GetNomenclatureBundle()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);            
            logger.Info("Получаем номенклатуру!");
            try
            {
                if (ProcessedBundle is null || string.IsNullOrWhiteSpace(ProcessedBundle.Contract) || ProcessedBundle.Contract != Contract)
                {
                    Dispatcher.UIThread.InvokeAsync(() => Order.Clear());
                    Dispatcher.UIThread.InvokeAsync(() => FoundNomenclaturePositions.Clear());
                    Dispatcher.UIThread.InvokeAsync(() => SelectedFoundPosition = null);
                    Dispatcher.UIThread.InvokeAsync(() => SelectedAddedPosition = new KeyValuePair<string, NomenclaturePosition>());
                    Dispatcher.UIThread.InvokeAsync(() => NomenclatureBundle = null);
                    Dispatcher.UIThread.InvokeAsync(() => ProcessedBundle = null);
                    GC.Collect();
                    try
                    {
                        var bundle = parser.Parse<Bundle>(ApiRequests.Get(CatalogRequestAddress));                        
                        Dispatcher.UIThread.InvokeAsync(() => NomenclatureBundle = bundle);
                        logger.Info("Номенклатура получена!");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"NomenclatureBundle: Не удалось получить номенклатуру!!!\r\n{ex}");
                        throw new Exception($"{ex}");
                    }
                    try
                    {
                        var processedBundle = new ProcessedBundle(NomenclatureBundle, Contract);
                        Dispatcher.UIThread.InvokeAsync(() => ProcessedBundle = processedBundle);
                        Dispatcher.UIThread.InvokeAsync(() => logger.Info("Номенклатура обработана!"));
                        Dispatcher.UIThread.InvokeAsync(() => Order.Contract = Contract);

                    }
                    catch (Exception ex)
                    {
                        logger.Error($"ProcessedBundle: Не удалось получить номенклатуру!!!\r\n{ex}");
                        throw new Exception($"{ex}");
                    }
                    logger.Info("Номенклатура записана!");
                }
                else
                {
                    logger.Info("Номенклатура уже была получена!");
                }
            }
            catch (Exception ex)
            {                
                logger.Error($"Не удалось получить номенклатуру по причине:\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => TextToFind = $"Не удалось получить номенклатуру по причине:\r\n{ex}");
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        private void FindNomenclature()
        {
            Dispatcher.UIThread.InvokeAsync(() => FoundNomenclaturePositions.Clear());
            if (ProcessedBundle?.NomenclaturePositions != null)
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Ищем '{TextToFind}'"));
                try
                {
                    Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"Найдено позиций номенклатуры - {ProcessedBundle?.NomenclaturePositions?.Count}"));
                    Dispatcher.UIThread.InvokeAsync(() => FoundNomenclaturePositions.AddRange(ProcessedBundle.NomenclaturePositions.Where(x =>
                        x.ActivityDefinition.Value.Identifier.Where(y => y.System == @"https://helix.ru/codes/nomenclature" && y.Value.ToUpper().Contains(TextToFind.ToUpper())).Count() > 0
                        || x.ActivityDefinition.Value.Title != null && x.ActivityDefinition.Value.Title.ToUpper().Contains(TextToFind.ToUpper())
                        || x.ActivityDefinition.Value.Description != null && x.ActivityDefinition.Value.Description.ToString().ToUpper().Contains(TextToFind.ToUpper())).ToList()));
                    Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Найдено совпадений - {FoundNomenclaturePositions?.Count}"));
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(() => TextToFind = $"Поиск исследования не удался по причине: {ex}");
                    logger.Error($"Поиск не удался по причине:\r\n{ex}");
                }
                finally
                {
                    Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
                }
            }
        }

        private void AddPosition()
        {
            logger.Trace("Добавляем позицию");
            string guid = Guid.NewGuid().ToString();
            Dispatcher.UIThread.InvokeAsync(() => Order.Positions.Add(new KeyValuePair<string, NomenclaturePosition>(guid, new NomenclaturePosition(SelectedFoundPosition))));
            Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"Добавлена позиция {Order.Positions.Where(x => x.Key == guid).FirstOrDefault()}"));
            Dispatcher.UIThread.InvokeAsync(() => Order.Positions = new List<KeyValuePair<string, NomenclaturePosition>>(Order.Positions));
        }

        private void RemovePosition()
        {
            Dispatcher.UIThread.InvokeAsync(() => Order.Positions.Remove(SelectedAddedPosition));
            Dispatcher.UIThread.InvokeAsync(() => Order.Positions = new List<KeyValuePair<string, NomenclaturePosition>>(Order.Positions));
            GC.Collect();
        }

        private void RemoveAllPositions()
        {
            Dispatcher.UIThread.InvokeAsync(() => Order.Positions.Clear());
            Dispatcher.UIThread.InvokeAsync(() => Order.Positions = new List<KeyValuePair<string, NomenclaturePosition>>());
            GC.Collect();
        }

        private void GetPositionsBundle()
        {
            Dispatcher.UIThread.InvokeAsync(() => IsWaiting = true);
            logger.Info("Создаем бандл позиций заказа!");
            try
            {
                Dispatcher.UIThread.InvokeAsync(() => Order.MakePositionsBundleText());
                Dispatcher.UIThread.InvokeAsync(() => logger.Info($"Общее количество позиций в заказе {Order.Positions.Count}"));
                logger.Info("Бандл позиций заказа создан");
                Dispatcher.UIThread.InvokeAsync(() => logger.Trace($"\r\n{Order.PositionsBundle}"));

            }
            catch (Exception ex)
            {
                logger.Error($"Не удалось создать бандл позиций заказа!!!\r\n{ex}");
                Dispatcher.UIThread.InvokeAsync(() => TextToFind = $"Не удалось создать бандл позиций заказа!!!\r\n{ex}");                
            }
            finally
            {
                Dispatcher.UIThread.InvokeAsync(() => IsWaiting = false);
            }
        }

        #endregion



    }
}
