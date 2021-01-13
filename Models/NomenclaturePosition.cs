using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveUI;

namespace MakeOrderR4v2.Models
{
    public class NomenclaturePosition: ReactiveObject
    {
        #region Fields and Properties
        public string Name { get; private set; }
        public KeyValuePair<string, ActivityDefinition> ActivityDefinition { get; private set; }
        public KeyValuePair<string, CatalogEntry> CatalogEntry { get; private set; }
        public Dictionary<string, SpecimenDefinition> SpecimenDefinitions { get; private set; }
        public Dictionary<string, SpecimenDefinition> RequiredSpecimens 
        {
            get
            {
                if (SpecimenDefinitions != null && SpecimenDefinitions.Count > 0)
                {
                    return SpecimenDefinitions
                    .Where(x => x.Value.Extension?
                        .Where(y => y.Url == @"https://helix.ru/extension/required" && y.Value.ToString() == "true")
                        .Count() > 0)?
                    .ToDictionary(x => x.Key, x => x.Value);
                }
                return null;
            }
        }
        public Dictionary<string, SpecimenDefinition> OptionalSpecimens
        {
            get
            {
                if (SpecimenDefinitions != null && SpecimenDefinitions.Count > 0)
                {
                    return SpecimenDefinitions
                    .Where(x => x.Value.Extension?
                        .Where(y => y.Url == @"https://helix.ru/extension/required" && y.Value.ToString() == "false")
                        .Count() > 0)?
                    .ToDictionary(x => x.Key, x => x.Value);
                }
                return null;
            }
        }

        private readonly List<SpecimenDefinition> specimensToSelect = new List<SpecimenDefinition>();
        public List<SpecimenDefinition> SpecimensToSelect
        {
            get
            {
                if (OptionalSpecimens != null && OptionalSpecimens.Count > 1)
                {
                    return OptionalSpecimens.Select(x => x.Value).ToList();
                }
                return specimensToSelect;
            }
        }

        private List<SpecimenDefinition> selectedSpecimens = new List<SpecimenDefinition>();
        public List<SpecimenDefinition> SelectedSpecimens
        {
            get
            {
                if (IsOnlyRequiredSpecimens)
                {

                    selectedSpecimens.Clear();
                    foreach (var spec in OptionalSpecimens)
                    {
                        selectedSpecimens.Add(spec.Value);
                    }
                }
                return selectedSpecimens;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref selectedSpecimens, value);
                this.RaisePropertyChanged("IsAllSpecimensSelected");
            }
        }

        private bool isAllSpecimensSelected;
        public bool IsAllSpecimensSelected
        {
            get
            {
                return isAllSpecimensSelected;
            }
            set
            {
                isAllSpecimensSelected = IsOnlyRequiredSpecimens || (!IsOnlyRequiredSpecimens && SelectedSpecimens.Count > 0);
            }
        }

        private Bundle bundle;
        public Bundle PositionBundle
        {
            get
            {
                if (bundle is null)
                {
                    bundle = new Bundle()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = Bundle.BundleType.Collection,
                        Entry = new List<Bundle.EntryComponent>()
                        {
                            new Bundle.EntryComponent() { FullUrl = ActivityDefinition.Key, Resource = ActivityDefinition.Value },
                            new Bundle.EntryComponent() { FullUrl = CatalogEntry.Key, Resource = CatalogEntry.Value }
                        }
                    };
                    foreach (var spec in SpecimenDefinitions)
                    {
                        bundle.Entry.Add(new Bundle.EntryComponent() { FullUrl = spec.Key, Resource = spec.Value });
                    }
                    foreach (var quest in Questionnaires)
                    {
                        bundle.Entry.Add(new Bundle.EntryComponent() { FullUrl = quest.Key, Resource = quest.Value });
                    }
                    foreach (var vSet in ValueSets)
                    {
                        bundle.Entry.Add(new Bundle.EntryComponent() { FullUrl = vSet.Key, Resource = vSet.Value });
                    }
                    foreach (var oDef in ObservationDefinitions)
                    {
                        bundle.Entry.Add(new Bundle.EntryComponent() { FullUrl = oDef.Key, Resource = oDef.Value });
                    }
                }
                else
                {
                    bundle.Id = Guid.NewGuid().ToString();
                }
                return bundle;
            }
        }

        public bool IsOnlyRequiredSpecimens 
        {
            get
            {
                return SpecimensToSelect.Count <= 1;
            }
        }

        private bool isExactlyOneSpecimen;
        public bool IsExactlyOneSpecimen
        {
            get
            {
                if (CatalogEntry.Value.AdditionalCharacteristic is null)
                {
                    isExactlyOneSpecimen = false;
                }
                else
                {
                    isExactlyOneSpecimen = CatalogEntry.Value.AdditionalCharacteristic
                        .Where(x => x.Coding?
                            .Where(y => y.System == @"https://helix.ru/codes/specimen-restrictions" && y.Code == @"exactly-one")?
                            .Count() > 0)?
                        .Count() > 0;
                }
                return isExactlyOneSpecimen;
            }
        }

        public Dictionary<string, ObservationDefinition> ObservationDefinitions { get; private set; }
        public Dictionary<string, Questionnaire> Questionnaires { get; private set; }
        public Dictionary<string, ValueSet> ValueSets { get; private set; }

        #endregion

        #region .ctor
        public NomenclaturePosition(KeyValuePair<string, ActivityDefinition> activityDefinition, KeyValuePair<string, CatalogEntry> catalogEntry, Dictionary<string, SpecimenDefinition> specimenDefinitions, Dictionary<string, Questionnaire> questionnaires, Dictionary<string, ValueSet> valueSets)
        {
            ActivityDefinition = activityDefinition;
            CatalogEntry = catalogEntry;
            SpecimenDefinitions = specimenDefinitions;
            Questionnaires = questionnaires;
            ValueSets = valueSets;
            ObservationDefinitions = new Dictionary<string, ObservationDefinition>();
            Name = ActivityDefinition.Value.Identifier.Where(x => x.System == @"https://helix.ru/codes/nomenclature").Select(y => y.Value).First() + " - " + ActivityDefinition.Value.Title;
        }
        public NomenclaturePosition(KeyValuePair<string, ActivityDefinition> activityDefinition, KeyValuePair<string, CatalogEntry> catalogEntry, Dictionary<string, SpecimenDefinition> specimenDefinitions, Dictionary<string, Questionnaire> questionnaires, Dictionary<string, ValueSet> valueSets, Dictionary<string, ObservationDefinition> observationDefinitions)
        {
            ActivityDefinition = activityDefinition;
            CatalogEntry = catalogEntry;
            SpecimenDefinitions = specimenDefinitions;
            Questionnaires = questionnaires;
            ValueSets = valueSets;
            ObservationDefinitions = observationDefinitions;
            Name = ActivityDefinition.Value.Identifier.Where(x => x.System == @"https://helix.ru/codes/nomenclature").Select(y => y.Value).First() + " - " + ActivityDefinition.Value.Title;
        }

        public NomenclaturePosition(NomenclaturePosition other)
        {
            if (other != null)
            {
                ActivityDefinition = other.ActivityDefinition;
                CatalogEntry = other.CatalogEntry;
                SpecimenDefinitions = other.SpecimenDefinitions;
                Questionnaires = other.Questionnaires;
                ValueSets = other.ValueSets;
                ObservationDefinitions = other.ObservationDefinitions;
                Name = ActivityDefinition.Value.Identifier.Where(x => x.System == @"https://helix.ru/codes/nomenclature").Select(y => y.Value).First() + " - " + ActivityDefinition.Value.Title;
            }            
        }
        #endregion

        #region Methods
        
        public override string ToString()
        {
            return ActivityDefinition.Value.Identifier.Where(x => x.System == @"https://helix.ru/codes/nomenclature").Select(y => y.Value).First() + " - " + ActivityDefinition.Value.Title;
        }
        #endregion
    }
}
