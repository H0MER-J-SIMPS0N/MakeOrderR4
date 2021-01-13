using Hl7.Fhir.Model;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Hl7.Fhir.Model.Questionnaire;
using static Hl7.Fhir.Model.QuestionnaireResponse;
using static Hl7.Fhir.Model.ValueSet;

namespace MakeOrderR4v2.Models
{
    public class QuestionnaireResponseAnswerElement : ReactiveObject
    {
        public string LinkId { get; set; }
        private string answer;
        public string Answer
        {
            get
            {
                return answer;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref answer, value);
                if (IsValid)
                {
                    ValidateColor = "LightGreen";
                }
                else if (Required)
                {
                    ValidateColor = "Red";
                }
                else
                {
                    ValidateColor = "Yellow";
                }
            }
        }

        private string validateColor;
        public string ValidateColor
        {
            get => validateColor;
            set
            {
                this.RaiseAndSetIfChanged(ref validateColor, value);
            }
        }

        public string Description { get; private set; }
        public string System { get; private set; }
        public bool Required { get; private set; }

        private List<ContainsComponent> answerItems;
        public List<ContainsComponent> AnswerItems
        {
            get
            {
                return answerItems;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref answerItems, value);                
            }
        }

        private ContainsComponent selectedAnswer;
        public ContainsComponent SelectedAnswer
        {
            get
            {
                return selectedAnswer;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref selectedAnswer, value);
                if (IsValid)
                {
                    ValidateColor = "LightGreen";
                }
                else if (Required)
                {
                    ValidateColor = "Red";
                }
                else
                {
                    ValidateColor = "Yellow";
                }
            }
        }
        public QuestionnaireItemType ItemType { get; set; }
        private string watermark;
        public string Watermark
        {
            get
            {
                if (ItemType is QuestionnaireItemType.Boolean)
                {
                    watermark = "Y или N";
                }
                else if (ItemType is QuestionnaireItemType.Date)
                {
                    watermark = "dd-MM-yyyy или dd.MM.yyyy";
                }
                else if (ItemType is QuestionnaireItemType.DateTime)
                {
                    watermark = "'dd-MM-yyyy HH:mm:ss' или 'dd.MM.yyyy HH:mm:ss'";
                }
                else if (ItemType is QuestionnaireItemType.Integer)
                {
                    watermark = "целое число";
                }
                else if (ItemType is QuestionnaireItemType.Decimal)
                {
                    watermark = "число с плавающей точкой";
                }
                else
                {
                    watermark = "Не менее одного непустого символа";
                }
                return watermark;
            }
        }
        private bool isValid;
        public bool IsValid
        {
            get
            {
                if (AnswerItems is null)
                {
                    isValid = !string.IsNullOrWhiteSpace(Answer);
                    if (isValid)
                    {
                        if (ItemType is QuestionnaireItemType.Boolean)
                        {
                            isValid = Answer.ToUpper() == "Y" || Answer.ToUpper() == "N";
                        }
                        else if (ItemType is QuestionnaireItemType.Integer)
                        {
                            isValid = int.TryParse(Answer, out _);
                        }
                        else if (ItemType is QuestionnaireItemType.Decimal)
                        {
                            isValid = decimal.TryParse(Answer, out _);
                        }
                        else if ((ItemType is QuestionnaireItemType.Date) && new Regex("(\\d{2}[-]{1,1}\\d{2}[-]{1,1}\\d{4})|(\\d{2}[.]{1,1}\\d{2}[.]{1,1}\\d{4})").IsMatch(Answer))
                        {
                            isValid = DateTime.TryParse(Answer, out _);
                        }
                        else if ((ItemType is QuestionnaireItemType.DateTime) && new Regex("(\\d{1,2}[-]{1,1}\\d{1,2}[-]{1,1}\\d{4,4} \\d{1,2}:\\d{1,2}:\\d{0,2})|(\\d{1,2}[.]{1,1}\\d{1,2}[.]{1,1}\\d{4,4} \\d{1,2}:\\d{1,2}:\\d{0,2})").IsMatch(Answer))
                        {
                            isValid = DateTime.TryParse(Answer, out _);
                        }
                        else if (ItemType is QuestionnaireItemType.String && Answer.Length > 0)
                        {
                            isValid = true;
                        }
                        else
                        {
                            isValid = false;
                        }
                    }
                }
                else
                {
                    isValid = SelectedAnswer != null;
                }
                return isValid;
            }
        }

        public QuestionnaireResponseAnswerElement(string linkId, string description, QuestionnaireItemType itemType, List<ContainsComponent> answerItems, string system, bool? required)
        {
            LinkId = linkId;
            ItemType = itemType;
            Description = description;
            AnswerItems = answerItems;
            System = system;
            Required = required == true;
            if (IsValid)
            {
                ValidateColor = "LightGreen";
            }
            else if (Required)
            {
                ValidateColor = "Red";
            }
            else
            {
                ValidateColor = "Yellow";
            }
        }

        public QuestionnaireResponse.ItemComponent MakeAnswerItemComponent()
        {
            Element value;
            if (AnswerItems is null)
            {
                if (ItemType == QuestionnaireItemType.Boolean)
                {
                    try
                    {
                        value = new FhirBoolean(Answer.ToUpper() == "Y");
                    }
                    catch
                    {
                        value = new FhirBoolean(null);
                    }
                }
                else if (ItemType == QuestionnaireItemType.Date)
                {
                    try
                    {
                        value = new FhirDateTime(DateTime.Parse(Answer).ToString("yyyy-MM-dd"));
                    }
                    catch
                    {
                        value = new FhirDateTime(DateTime.Now.AddYears(-115).ToString("yyyy-MM-dd"));
                    }
                }
                else if (ItemType == QuestionnaireItemType.DateTime)
                {
                    try
                    {
                        value = new FhirDateTime(DateTime.Parse(Answer).ToString("o") + DateTime.Now.ToString("%K"));
                    }
                    catch
                    {
                        value = new FhirDateTime(DateTime.Now.AddYears(-115).ToString("o") + DateTime.Now.ToString("%K"));
                    }
                }
                else if (ItemType == QuestionnaireItemType.Integer)
                {
                    try
                    {
                        value = new Integer(int.Parse(Answer));
                    }
                    catch
                    {
                        value = new Integer(-1);
                    }
                }
                else if (ItemType == QuestionnaireItemType.Decimal)
                {
                    try
                    {
                        value = new FhirDecimal(decimal.Parse(Answer));
                    }
                    catch
                    {
                        value = new FhirDecimal(-1);
                    }
                }
                else
                {
                    value = new FhirString(Answer);
                }
            }
            else
            {
                value = new Coding()
                {
                    Code = SelectedAnswer?.Code,
                    System = System
                };
            }
            return new QuestionnaireResponse.ItemComponent()
            {
                LinkId = LinkId,
                Answer = new List<AnswerComponent>()
                {
                    new AnswerComponent()
                    {
                        Value = value
                    }
                }
            };
        }
    }
}
