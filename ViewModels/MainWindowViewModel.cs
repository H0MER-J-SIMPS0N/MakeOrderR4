using MakeOrderR4v2.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MakeOrderR4v2.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        #region Fields and Properties
        public ObservableCollection<ViewModelBase> ViewModels { get; set; }

        private ViewModelBase selectedViewModel;
        public Order Order { get; set; }
        public ViewModelBase SelectedViewModel
        {
            get => selectedViewModel;
            set
            {
                if (selectedViewModel != null)
                {
                    selectedViewModel.IsSelected = false;
                }
                this.RaiseAndSetIfChanged(ref selectedViewModel, value);
                selectedViewModel.IsSelected = true;
            }
                
        }
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            if (GetSettings.Get() is null)
            {                
                throw new Exception("Не прочитать настройки!");
            }
            Order = new Order();
            ViewModels = new ObservableCollection<ViewModelBase>(new List<ViewModelBase>()
            {
                new NomenclatureViewModel(Order),
                new SelectSamplesViewModel(Order),
                new PreanalyticsViewModel(Order),
                new QuestionnaireViewModel(Order),
                new PatientViewModel(Order),
                new AssembleOrderViewModel(Order)
            });
            SelectedViewModel = ViewModels is null || ViewModels.Count == 0 ? null : ViewModels[0];
        }
        #endregion
    }
}
