using Hl7.Fhir.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MakeOrderR4v2.Models
{
    public class ProcessedBundle
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public string Contract { get; private set; }
        public Dictionary<string, CatalogEntry> CatalogEntries { get; private set; } = new Dictionary<string, CatalogEntry>();
        public Dictionary<string, ActivityDefinition> ActivityDefinitions { get; private set; } = new Dictionary<string, ActivityDefinition>();
        public Dictionary<string, SpecimenDefinition> SpecimenDefinitions { get; private set; } = new Dictionary<string, SpecimenDefinition>();
        public Dictionary<string, Questionnaire> Questionnaires { get; private set; } = new Dictionary<string, Questionnaire>();
        public Dictionary<string, ValueSet> ValueSets { get; private set; } = new Dictionary<string, ValueSet>();
        public Dictionary<string, ObservationDefinition> ObservationDefinitions { get; private set; } = new Dictionary<string, ObservationDefinition>();

        public List<NomenclaturePosition> NomenclaturePositions { get; private set; } = new List<NomenclaturePosition>();
        #endregion

        #region .ctor
        public ProcessedBundle(Bundle nomenclatureBundle, string contract)
        {
            if (nomenclatureBundle.Type == Bundle.BundleType.Collection)
            {
                Contract = contract;
                int count = nomenclatureBundle.Entry.Count;
                for (int i = 0; i < count; i++)
                {
                    if (nomenclatureBundle.Entry[i].Resource is CatalogEntry)
                    {
                        CatalogEntries.Add(nomenclatureBundle.Entry[i].FullUrl, nomenclatureBundle.Entry[i].Resource as CatalogEntry);
                    }
                    if (nomenclatureBundle.Entry[i].Resource is ActivityDefinition)
                    {
                        ActivityDefinitions.Add(nomenclatureBundle.Entry[i].FullUrl, nomenclatureBundle.Entry[i].Resource as ActivityDefinition);
                    }
                    if (nomenclatureBundle.Entry[i].Resource is SpecimenDefinition)
                    {
                        SpecimenDefinitions.Add(nomenclatureBundle.Entry[i].FullUrl, nomenclatureBundle.Entry[i].Resource as SpecimenDefinition);
                    }
                    if (nomenclatureBundle.Entry[i].Resource is Questionnaire)
                    {
                        Questionnaires.Add(nomenclatureBundle.Entry[i].FullUrl, nomenclatureBundle.Entry[i].Resource as Questionnaire);
                    }
                    if (nomenclatureBundle.Entry[i].Resource is ValueSet)
                    {
                        ValueSets.Add(nomenclatureBundle.Entry[i].FullUrl, nomenclatureBundle.Entry[i].Resource as ValueSet);
                    }
                    if (nomenclatureBundle.Entry[i].Resource is ObservationDefinition)
                    {
                        ObservationDefinitions.Add(nomenclatureBundle.Entry[i].FullUrl, nomenclatureBundle.Entry[i].Resource as ObservationDefinition);
                    }
                }
                KeyValuePair<string, CatalogEntry> catEntry;
                Dictionary<string, SpecimenDefinition> specDefs;
                Dictionary<string, Questionnaire> quests;
                Dictionary<string, ValueSet> valSets;                
                foreach (var actDef in ActivityDefinitions)
                {
                    catEntry = new KeyValuePair<string, CatalogEntry>();
                    catEntry = CatalogEntries.Where(x => x.Value.ReferencedItem.Reference.ToString() == actDef.Key).FirstOrDefault();
                    specDefs = new Dictionary<string, SpecimenDefinition>();
                    foreach (var specDef in SpecimenDefinitions)
                    {
                        if (actDef.Value.SpecimenRequirement.Where(y => y.ReferenceElement.ToString() == specDef.Key).Count() > 0)
                        {
                            specDefs.TryAdd(specDef.Key, specDef.Value);
                        }
                    }
                    quests = new Dictionary<string, Questionnaire>();
                    foreach (var quest in Questionnaires)
                    {
                        if (actDef.Value.Extension?.Where(x => x.Url == @"http://hl7.org/fhir/StructureDefinition/servicerequest-questionnaireRequest" && (x.Value as ResourceReference).Reference.ToString() == quest.Key).Count() > 0)
                        {
                            quests.TryAdd(quest.Key, quest.Value);
                        }
                    }
                    valSets = new Dictionary<string, ValueSet>();
                    if (quests.Count > 0)
                    {                        
                        foreach (var quest in quests)
                        {
                            if (quest.Value.Item.Where(x => x.AnswerValueSet != null).Count() > 0)
                            {
                                foreach (var item in quest.Value.Item.Where(x => x.AnswerValueSet != null))
                                {
                                    valSets.TryAdd(item.AnswerValueSet, ValueSets[item.AnswerValueSet]);
                                }
                            }
                        }
                    }
                    NomenclaturePositions.Add(new NomenclaturePosition(actDef, catEntry, specDefs, quests, valSets));
                }
            }
        }
        #endregion
    }
}
