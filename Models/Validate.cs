using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MakeOrderR4v2.Models
{
    static class Validate
    {
        #region Methods
        public static bool Contract(string contract)
        {
            return !string.IsNullOrWhiteSpace(contract) && Regex.IsMatch(contract, @"^[Cc]{1}\d{9}$");
        }

        public static bool Text(string text)
        {
            return !string.IsNullOrWhiteSpace(text) && text.Length > 2;
        }

        public static bool AddNomenclature(NomenclaturePosition selectedFoundNomenclature, List<KeyValuePair<string, NomenclaturePosition>> addedNomenclature)
        {
            bool isSelectedOrderable = selectedFoundNomenclature.CatalogEntry.Value.Orderable == true;
            bool isManyTimes = selectedFoundNomenclature.CatalogEntry.Value.AdditionalCharacteristic
                            .Where(y => y.Coding
                                .Where(z => z.System == @"https://helix.ru/codes/nomenclature-restrictions")
                                .Count() == 0)
                            .Count() == selectedFoundNomenclature.CatalogEntry.Value.AdditionalCharacteristic.Count();
            string hxid = selectedFoundNomenclature.ActivityDefinition.Value.Identifier.Where(x => x.System == @"https://helix.ru/codes/nomenclature").Select(y => y.Value).FirstOrDefault();
            bool isAlreadyAdded = addedNomenclature is null || addedNomenclature.Count == 0 || addedNomenclature
                .Where(x => x.Value.ActivityDefinition.Value.Identifier.Where(y => y.System == @"https://helix.ru/codes/nomenclature").Select(y => y.Value).FirstOrDefault() == hxid)
                .Count() > 0;
            //logger.Debug($"isSelectedOrderable - {isSelectedOrderable}; isManyTimes - {isManyTimes}; (!isManyTimes && !isAlreadyAdded) - {!isManyTimes} && {!isAlreadyAdded}");
            return isSelectedOrderable && (isManyTimes || (!isManyTimes && !isAlreadyAdded));
        }

        public static bool CreatePreanalyticsRequest(ICollection<KeyValuePair<string, NomenclaturePosition>> exactlyOneSpecimensNomenclature, ICollection<KeyValuePair<string, NomenclaturePosition>> manySpecimensNomenclature)
        {
            if (exactlyOneSpecimensNomenclature.Count > 0)
            {
                if (manySpecimensNomenclature.Count > 0)
                {
                    return exactlyOneSpecimensNomenclature.Where(x => x.Value.IsAllSpecimensSelected).Count() == exactlyOneSpecimensNomenclature.Count
                        && manySpecimensNomenclature.Where(x => x.Value.IsAllSpecimensSelected).Count() == manySpecimensNomenclature.Count;
                }
                else
                {
                    return exactlyOneSpecimensNomenclature.Where(x => x.Value.IsAllSpecimensSelected).Count() == exactlyOneSpecimensNomenclature.Count;
                }
            }
            else if (manySpecimensNomenclature.Count > 0)
            {
                return manySpecimensNomenclature.Where(x => x.Value.IsAllSpecimensSelected).Count() == manySpecimensNomenclature.Count;
            }
            return true;
        }

        public static bool AreAllAnswersValid(List<QuestionnaireResponseAnswerElement> questionnaireResponseAnswerElements)
        {
            return questionnaireResponseAnswerElements.Where(x => x.ValidateColor == "LightGreen" || x.ValidateColor == "Yellow").Count() == questionnaireResponseAnswerElements.Count;
        }

        #endregion

    }
}
