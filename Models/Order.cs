using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NLog;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MakeOrderR4v2.Models
{
    public class Order: ReactiveObject
    {
        #region Fields and Properties
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly FhirJsonSerializer serializer = new FhirJsonSerializer(new SerializerSettings() { Pretty = true });
        private static readonly FhirJsonParser parser = new FhirJsonParser();
        private List<KeyValuePair<string, NomenclaturePosition>> positions = new List<KeyValuePair<string, NomenclaturePosition>>();
        public List<KeyValuePair<string, NomenclaturePosition>> Positions
        {
            get => positions;
            set
            {
                this.RaiseAndSetIfChanged(ref positions, value);
                MakePositionsBundleText();                
            }
        }
        public string Contract { get; set; }
        private string patientText;
        public string PatientText
        {
            get => patientText;
            set => this.RaiseAndSetIfChanged(ref patientText, value);
        }

        private string positionsBundle;
        public string PositionsBundle
        {
            get => positionsBundle;
            set
            {
                this.RaiseAndSetIfChanged(ref positionsBundle, value);
                СreatePreanalyticsRequestBundleText();
                CreateBundleForQuestionnaireText();
                ResultBundle = string.Empty;
            }
        }
        private string preanalyticsRequestBundle;
        public string PreanalyticsRequestBundle
        {
            get => preanalyticsRequestBundle;
            set
            {
                this.RaiseAndSetIfChanged(ref preanalyticsRequestBundle, value);
                PreanalyticsBundle = string.Empty;
            }
        }
        private string preanalyticsBundle;
        public string PreanalyticsBundle
        {
            get => preanalyticsBundle;
            set => this.RaiseAndSetIfChanged(ref preanalyticsBundle, value);
        }
        private string bundleForQuestionnaire;
        public string BundleForQuestionnaire
        {
            get => bundleForQuestionnaire;
            set
            {
                this.RaiseAndSetIfChanged(ref bundleForQuestionnaire, value);
                QuestionnaireBundleWithAnswers = string.Empty;
            }
        }
        private string questionnaireBundleWithAnswers;
        public string QuestionnaireBundleWithAnswers
        {
            get => questionnaireBundleWithAnswers;
            set
            {
                this.RaiseAndSetIfChanged(ref questionnaireBundleWithAnswers, value);
                QuestionnaireResponseItems?.Clear();
            }
        }
        private List<QuestionnaireResponseAnswerElement> questionnaireResponseItems;
        public List<QuestionnaireResponseAnswerElement> QuestionnaireResponseItems 
        {
            get => questionnaireResponseItems;
            set => this.RaiseAndSetIfChanged(ref questionnaireResponseItems, value);
        }
        private string resultBundle;
        public string ResultBundle
        {
            get => resultBundle;
            set => this.RaiseAndSetIfChanged(ref resultBundle, value);
        }

        #endregion

        #region .ctor
        public Order()
        {

        }
        #endregion

        #region Methods

        public void MakePositionsBundleText()
        {
            if (Positions != null && Positions.Count > 0)
            {
                Bundle BundleList = new Bundle() { Type = Bundle.BundleType.Collection };
                foreach (var op in Positions)
                {
                    BundleList.AddResourceEntry(op.Value.PositionBundle, "urn:uuid:" + op.Value.PositionBundle.Id);
                }
                PositionsBundle = serializer.SerializeToString(BundleList);
            }
            else
            {
                PositionsBundle = string.Empty;
            }
        }

        public void СreatePreanalyticsRequestBundleText()
        {
            ResultBundle = string.Empty;
            if (string.IsNullOrEmpty(PositionsBundle))
            {
                PreanalyticsRequestBundle = string.Empty;
                BundleForQuestionnaire = string.Empty;
                return;
            }
            Bundle bundle = new Bundle() { Type = Bundle.BundleType.Collection, Entry = new List<Bundle.EntryComponent>() };
            bundle.Entry.Add(
                new Bundle.EntryComponent()
                {
                    FullUrl = "urn:uuid:" + Guid.NewGuid().ToString(),
                    Resource = new Contract()
                    {
                        Identifier = new List<Identifier>()
                        {
                            new Identifier("https://helix.ru/codes/contract", Contract)
                        }
                    }
                });
            foreach (var position in Positions)
            {
                var specReq = new List<ResourceReference>();
                foreach (var sd in position.Value.SelectedSpecimens ??= new List<SpecimenDefinition>())
                {
                    bundle.Entry.Add(
                        new Bundle.EntryComponent()
                        {
                            FullUrl = "urn:uuid:" + sd.Id,
                            Resource = new SpecimenDefinition()
                            {
                                Identifier = new Identifier("https://api.medlinx.online/extra/supportingInfo", sd.Identifier.Value)
                            }
                        }
                        );
                    specReq.Add(new ResourceReference("urn:uuid:" + sd.Id));
                }
                foreach (var sd in position.Value.RequiredSpecimens)
                {
                    bundle.Entry.Add(
                        new Bundle.EntryComponent()
                        {
                            FullUrl = "urn:uuid:" + sd.Value.Id,
                            Resource = new SpecimenDefinition()
                            {
                                Identifier = new Identifier("https://api.medlinx.online/extra/supportingInfo", sd.Value.Identifier.Value)
                            }
                        }
                        );
                    specReq.Add(new ResourceReference("urn:uuid:" + sd.Value.Id));
                }
                bundle.Entry.Add(
                    new Bundle.EntryComponent()
                    {
                        FullUrl = "urn:uuid:" + position.Value.ActivityDefinition.Value.Id,
                        Resource = new ActivityDefinition()
                        {
                            Status = PublicationStatus.Active,
                            Identifier = position.Value.ActivityDefinition.Value.Identifier,
                            SpecimenRequirement = specReq
                        }
                    });
            }
            PreanalyticsRequestBundle = serializer.SerializeToString(bundle);
        }

        public void CreateBundleForQuestionnaireText()
        {
            if (string.IsNullOrEmpty(PositionsBundle))
            {
                BundleForQuestionnaire = string.Empty;
                return;
            }
            QuestionnaireBundleWithAnswers = string.Empty;
            QuestionnaireResponseItems?.Clear();
            Dictionary<string, Questionnaire> questionnaires = new Dictionary<string, Questionnaire>();
            Dictionary<string, ValueSet> valueSets = new Dictionary<string, ValueSet>();

            foreach (var position in Positions)
            {
                foreach (var questionnaire in position.Value.Questionnaires)
                {
                    if (!questionnaires.Keys.Contains(questionnaire.Key))
                    {
                        questionnaires.Add(questionnaire.Key, questionnaire.Value);
                    }
                }
                foreach (var valueSet in position.Value.ValueSets)
                {
                    if (!valueSets.Keys.Contains(valueSet.Key))
                    {
                        valueSets.Add(valueSet.Key, valueSet.Value);
                    }
                }
            }
            List<Questionnaire.ItemComponent> items = new List<Questionnaire.ItemComponent>();
            foreach (var q in questionnaires)
            {
                foreach (var item in q.Value.Item)
                {
                    if (items.Where(x => x.LinkId == item.LinkId).Count() == 0)
                    {
                        items.Add(item);
                    }
                }
            }
            items.Add(new Questionnaire.ItemComponent()
            {
                LinkId = "X_PRACTITIONER_ID",
                Text = "Id врача",
                Type = Questionnaire.QuestionnaireItemType.String,
                Required = false
            }
            );
            items.Add(new Questionnaire.ItemComponent()
            {
                LinkId = "X_CLINICAL_RECORD",
                Text = "Номер карты клиента",
                Type = Questionnaire.QuestionnaireItemType.String,
                Required = false
            }
            );
            Bundle.EntryComponent resultQuestionnaire = new Bundle.EntryComponent()
            {
                FullUrl = "urn:uuid:" + Guid.NewGuid().ToString(),
                Resource = new Questionnaire()
                {
                    Id = Guid.NewGuid().ToString(),
                    Status = PublicationStatus.Active,
                    Item = items
                }
            };
            Bundle resultBundle = new Bundle() { Entry = new List<Bundle.EntryComponent>() };
            resultBundle.Entry.Add(resultQuestionnaire);
            if (valueSets.Count > 0)
            {
                resultBundle.Entry.AddRange(valueSets.Select(x => new Bundle.EntryComponent() { FullUrl = x.Key, Resource = x.Value }));
            }
            BundleForQuestionnaire = serializer.SerializeToString(resultBundle);
        }

        public List<QuestionnaireResponseAnswerElement> GetQuestionnaireResponseItems()
        {
            var bundle = parser.Parse<Bundle>(BundleForQuestionnaire);
            List<QuestionnaireResponseAnswerElement> items = new List<QuestionnaireResponseAnswerElement>();
            List<Questionnaire.ItemComponent> bundleItems = bundle.Entry.Select(x => x.Resource as Questionnaire).FirstOrDefault().Item;
            List<Bundle.EntryComponent> valueSets = bundle.Entry.Where(x => x.Resource is ValueSet).ToList();
            ValueSet tempValueSet;
            foreach (var item in bundleItems)
            {
                tempValueSet = valueSets != null ? valueSets.Where(x => x.FullUrl == item.AnswerValueSet)?.FirstOrDefault()?.Resource as ValueSet : null;
                items.Add(new QuestionnaireResponseAnswerElement(item.LinkId, item.Text, (Questionnaire.QuestionnaireItemType)item.Type, tempValueSet?.Expansion?.Contains, tempValueSet is null ? string.Empty : tempValueSet.Url, item.Required));
            }
            return items;
        }

        public void CreateBundleWithAnswers(List<QuestionnaireResponseAnswerElement> answerItems)
        {
            QuestionnaireResponse response = new QuestionnaireResponse()
            {
                Id = Guid.NewGuid().ToString(),
                Status = QuestionnaireResponse.QuestionnaireResponseStatus.Completed,
                Item = answerItems.Where(x => !string.IsNullOrWhiteSpace(x.Answer) || x.SelectedAnswer != null).Select(x => x.MakeAnswerItemComponent()).ToList()
            };
            QuestionnaireBundleWithAnswers = serializer.SerializeToString(response);
        }

        public void CreateResultBundle()
        {
            Bundle preanBundle = parser.Parse<Bundle>(PreanalyticsBundle);
            Dictionary<ServiceRequest, List<string>> serviceRequestsToSpecimenReference = new Dictionary<ServiceRequest, List<string>>();
            List<Bundle.EntryComponent> innerBundleEntries = new List<Bundle.EntryComponent>();
            Bundle.EntryComponent patient;
            try
            {
                patient = new Bundle.EntryComponent()
                {
                    FullUrl = "urn:uuid:" + Guid.NewGuid().ToString(),
                    Resource = parser.Parse<Patient>(PatientText)
                };
            }
            catch (Exception ex)
            {
                logger.Error($"Не удалось распарсить пациента по причине:\r\n{ex}");
                ResultBundle = $"Не удалось распарсить пациента по причине:\r\n{ex}";
                return;
            }            
            innerBundleEntries.Add(patient);
            for (int i = 0; i < preanBundle.Entry.Count; i++)
            {
                if (preanBundle.Entry[i].Resource.TypeName == "ServiceRequest")
                {
                    ServiceRequest sr = preanBundle.Entry[i].Resource as ServiceRequest;
                    innerBundleEntries.Add(
                                    new Bundle.EntryComponent()
                                    {
                                        FullUrl = preanBundle.Entry[i].FullUrl,
                                        Resource = new ServiceRequest()
                                        {
                                            Requisition = new Identifier("my.system.org", Guid.NewGuid().ToString()),
                                            SupportingInfo = new List<ResourceReference>()
                                        {
                                new ResourceReference()
                                {
                                    Identifier = new Identifier(@"https://helix.ru/codes/contract", Contract)
                                }
                                        },
                                            Intent = RequestIntent.Order,
                                            Status = RequestStatus.Active,
                                            Code = sr.Code,
                                            Subject = new ResourceReference(patient.FullUrl),
                                            Occurrence = new FhirDateTime(DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss+03:00")),
                                            Specimen = sr.Specimen
                                        }
                                    });
                }
                if (preanBundle.Entry[i].Resource.TypeName == "Specimen")
                {
                    Specimen sp = preanBundle.Entry[i].Resource as Specimen;
                    innerBundleEntries.Add(
                        new Bundle.EntryComponent()
                        {
                            FullUrl = preanBundle.Entry[i].FullUrl,
                            Resource = new Specimen()
                            {
                                Type = new CodeableConcept()
                                {
                                    Coding = sp.Type.Coding
                                },
                                Subject = new ResourceReference(patient.FullUrl),
                                Collection = new Specimen.CollectionComponent()
                                {
                                    Collected = new FhirDateTime(DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss+03:00"))
                                },
                                Container = new List<Specimen.ContainerComponent>()
                                {
                                    new Specimen.ContainerComponent()
                                    {
                                        Identifier = sp.Identifier is null ? new List<Identifier>() { new Identifier(@"https://helix.ru/codes/labels", "0000000000") } : sp.Identifier,
                                        Type = sp.Container.Where(x => x.Type.Coding.Where(y => y.System == @"https://api.medlinx.online/terminology/specimen-container-type").Count() > 0).Select(z => z.Type).FirstOrDefault(),
                                        SpecimenQuantity = new SimpleQuantity()
                                        {
                                            Value = sp.Collection.Quantity.Value
                                        }
                                    }
                                },
                                Extension = new List<Extension>()
                                {
                                    new Extension()
                                    {
                                        Url = @"https://api.medlinx.online/extra/supportingInfo",
                                        Value = new FhirString(sp.Extension.Where(x => x.Url == @"https://api.medlinx.online/extra/supportingInfo").Select(y => y.Value.ToString()).FirstOrDefault())
                                    }
                                }
                            }
                        });
                }
            }
            if (!string.IsNullOrWhiteSpace(QuestionnaireBundleWithAnswers) && QuestionnaireBundleWithAnswers.Length > 0)
            {
                try
                {
                    QuestionnaireResponse qr = parser.Parse<QuestionnaireResponse>(QuestionnaireBundleWithAnswers);
                    innerBundleEntries.Add(new Bundle.EntryComponent() { FullUrl = "urn:uuid:" + qr.Id, Resource = qr });
                }
                catch (Exception ex)
                {
                    logger.Error($"Не удалось распарсить или добавить к бандлу QuestionnaireResponse:\r\n{QuestionnaireBundleWithAnswers}\r\nПо причине:\r\n{ex}");
                }
                
            }
            Bundle resultBundle = new Bundle()
            {
                Type = Bundle.BundleType.Transaction,
                Entry = new List<Bundle.EntryComponent>()
            };
            Bundle innerBundle = new Bundle()
            {
                Type = Bundle.BundleType.Collection,
                Meta = new Meta()
                {
                    Security = new List<Coding>()
                    {
                        new Coding("read", "service"),
                        new Coding("updatebody", "service")
                    }
                },
                Entry = innerBundleEntries
            };

            string innerBundleFullUrl = "urn:uuid:" + Guid.NewGuid().ToString();

            Task task = new Task()
            {
                Intent = Task.TaskIntent.Order,
                Status = Task.TaskStatus.Requested,
                Meta = new Meta()
                {
                    Security = new List<Coding>()
                    {
                        new Coding("read", "service"),
                        new Coding("updatebody", "service")
                    }
                },
                Code = new CodeableConcept()
                {
                    Coding = new List<Coding>()
                    {
                        new Coding(@"https://api.medlinx.online/terminology/task_type", "OrderProcessingTask")
                    }
                },
                Input = new List<Task.ParameterComponent>()
                {
                    new Task.ParameterComponent()
                    {
                        Type = new CodeableConcept()
                        {
                            Coding = new List<Coding>()
                            {
                                new Coding() { System = @"https://api.medlinx.online/terminology/order-bundle" }
                            }
                        },
                        Value = new ResourceReference(innerBundleFullUrl)
                    }
                }
            };
            resultBundle.Entry.Add(new Bundle.EntryComponent() { Request = new Bundle.RequestComponent() { Method = Bundle.HTTPVerb.POST, Url = "Bundle" }, FullUrl = innerBundleFullUrl, Resource = innerBundle });
            resultBundle.Entry.Add(new Bundle.EntryComponent() { Request = new Bundle.RequestComponent() { Method = Bundle.HTTPVerb.POST, Url = "Task" }, FullUrl = "urn:uuid:" + Guid.NewGuid().ToString(), Resource = task });
            ResultBundle = serializer.SerializeToString(resultBundle);
        }

        public void Clear()
        {
            Positions = new List<KeyValuePair<string, NomenclaturePosition>>();
            Contract = string.Empty;
            PositionsBundle = string.Empty;
            PreanalyticsRequestBundle = string.Empty;
            PreanalyticsBundle = string.Empty;
            BundleForQuestionnaire = string.Empty;
            QuestionnaireBundleWithAnswers = string.Empty;
            ResultBundle = string.Empty;
            QuestionnaireResponseItems = new List<QuestionnaireResponseAnswerElement>();
            GC.Collect();
        }
        #endregion
    }
}
