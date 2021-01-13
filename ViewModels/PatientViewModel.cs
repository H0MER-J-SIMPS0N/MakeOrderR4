using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using MakeOrderR4v2.Models;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace MakeOrderR4v2.ViewModels
{
    public class PatientViewModel : ViewModelBase
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public override string Name { get => "Пациент"; }
        private bool isSelected;
        public override bool IsSelected
        {
            get => isSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref isSelected, value);
            }
        }
        public string Patient { get; private set; }
        public Order Order { get; private set; }
        #endregion

        #region .ctor
        public PatientViewModel(Order order)
        {
            Order = order;
            Patient = new FhirJsonSerializer(new SerializerSettings() { Pretty = true }).SerializeToString(
                    new FhirJsonParser().Parse<Patient>("{\"resourceType\":\"Patient\",\"name\":[{\"family\":\"TESTOV\",\"given\":[\"TEST\",\"TESTOVICH\"]}],\"telecom\":[{\"system\":\"phone\",\"value\":\"\",\"use\":\"mobile\"},{\"system\":\"email\",\"value\":\"\"}],\"gender\":\"male\",\"birthDate\":\"1980-01-01\"}"));
            Order.PatientText = Patient;
        }
        
        #endregion




        }
}
