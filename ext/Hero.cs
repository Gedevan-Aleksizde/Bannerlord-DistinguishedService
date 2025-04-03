using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
namespace DistinguishedService.ext
{
    public static class HeroExt
    {
        public static void CheckInitialLevel(this Hero hero)
        {
            hero.SetupDefaultPoints();
            hero.SetInitialLevelFromSkills();
            hero.CheckLevel();
            hero.SetInitialFocusAndAttributePoints();
        }
        public static void SetupDefaultPoints(this Hero hero)
        {
            hero.HeroDeveloper.UnspentFocusPoints = (hero.Level - 1) * Campaign.Current.Models.CharacterDevelopmentModel.FocusPointsPerLevel + Campaign.Current.Models.CharacterDevelopmentModel.FocusPointsAtStart;
            hero.HeroDeveloper.UnspentAttributePoints = (hero.Level - 1) / Campaign.Current.Models.CharacterDevelopmentModel.LevelsPerAttributePoint + Campaign.Current.Models.CharacterDevelopmentModel.AttributePointsAtStart;
        }
        public static void SetInitialLevelFromSkills(this Hero hero)
        {
            int b = (int)Skills.All.Sum((SkillObject s) => 2f * MathF.Pow(hero.GetSkillValue(s), 2.2f)) - 2000;
            // hero.HeroDeveloper.TotalXp = MathF.Max(1, b); TODO: is really needed?
        }
        public static void CheckLevel(this Hero hero)  // shouldNotify arg is omitted
        {
            bool flag = false;
            int totalXp = hero.HeroDeveloper.TotalXp;
            while (!flag)
            {
                int xpRequiredForLevel = hero.HeroDeveloper.GetXpRequiredForLevel(hero.Level + 1);
                if (xpRequiredForLevel != Campaign.Current.Models.CharacterDevelopmentModel.GetMaxSkillPoint() && totalXp >= xpRequiredForLevel)
                {
                    hero.Level++;
                }
                else
                {
                    flag = true;
                }
            }
        }
        public static void SetInitialFocusAndAttributePoints(this Hero hero)
        {
            foreach (CharacterAttribute item in Attributes.All)
            {
                int attributeValue = hero.GetAttributeValue(item);
                hero.HeroDeveloper.UnspentAttributePoints -= attributeValue;
                if (attributeValue == 0)
                {
                    hero.HeroDeveloper.AddAttribute(item, 1);
                }
            }

            foreach (SkillObject item2 in Skills.All)
            {
                hero.HeroDeveloper.UnspentFocusPoints -= hero.HeroDeveloper.GetFocus(item2);
                hero.HeroDeveloper.InitializeSkillXp(item2);
            }
        }
        public static MBReadOnlyList<PerkObject> GetOneAvailablePerkForEachPerkPair(this Hero hero)
        {
            MBList<PerkObject> mBList = new();
            foreach (PerkObject item in PerkObject.All)
            {
                SkillObject skill = item.Skill;
                if ((float)hero.GetSkillValue(skill) >= item.RequiredSkillValue && !hero.GetPerkValue(item) && (item.AlternativePerk == null || !hero.GetPerkValue(item.AlternativePerk)) && !mBList.Contains(item.AlternativePerk))
                {
                    mBList.Add(item);
                }
            }

            return mBList;
        }
        public static void DevelopCharacterStats(this Hero hero)
        {
            hero.DistributeUnspentAttributePoints();
            hero.DistributeUnspentFocusPoints();
            hero.SelectPerks();
        }
        private static void DistributeUnspentAttributePoints(this Hero hero)
        {
            while (hero.HeroDeveloper.UnspentAttributePoints > 0)
            {
                CharacterAttribute characterAttribute = null;
                float num = float.MinValue;
                foreach (CharacterAttribute item in Attributes.All)
                {
                    int attributeValue = hero.GetAttributeValue(item);
                    if (attributeValue >= Campaign.Current.Models.CharacterDevelopmentModel.MaxAttribute)
                    {
                        continue;
                    }
                    float num2 = 0f;
                    if (attributeValue == 0)
                    {
                        num2 = float.MaxValue;
                    }
                    else
                    {
                        foreach (SkillObject skill in item.Skills)
                        {
                            float num3 = MathF.Max(0f, (float)(75 + hero.GetSkillValue(skill)) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(attributeValue, hero.HeroDeveloper.GetFocus(skill), null).ResultNumber);
                            num2 += num3;
                        }
                        int num4 = 1;
                        foreach (CharacterAttribute item2 in Attributes.All)
                        {
                            if (item2 != item)
                            {
                                int attributeValue2 = hero.GetAttributeValue(item2);
                                if (num4 < attributeValue2)
                                {
                                    num4 = attributeValue2;
                                }
                            }
                        }
                        float num5 = MathF.Sqrt((float)num4 / (float)attributeValue);
                        num2 *= num5;
                    }
                    if (num2 > num)
                    {
                        num = num2;
                        characterAttribute = item;
                    }
                }
                if (characterAttribute != null)
                {
                    hero.HeroDeveloper.AddAttribute(characterAttribute, 1);
                    continue;
                }
                break;
            }

        }
        private static void DistributeUnspentFocusPoints(this Hero hero)
        {
            while (hero.HeroDeveloper.UnspentFocusPoints > 0)
            {
                SkillObject skillObject = null;
                float num = float.MinValue;
                foreach (SkillObject item in Skills.All)
                {
                    if (hero.HeroDeveloper.CanAddFocusToSkill(item))
                    {
                        int attributeValue = hero.GetAttributeValue(item.CharacterAttribute);
                        int focus = hero.HeroDeveloper.GetFocus(item);
                        float num2 = (float)hero.GetSkillValue(item) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(attributeValue, focus, null).ResultNumber;
                        if (num2 > num)
                        {
                            num = num2;
                            skillObject = item;
                        }
                    }
                }
                if (skillObject != null)
                {
                    hero.HeroDeveloper.AddFocus(skillObject, 1);
                    continue;
                }
                break;
            }
        }
        private static void SelectPerks(this Hero hero)
        {
            foreach (PerkObject item in hero.GetOneAvailablePerkForEachPerkPair())
            {
                if (item.AlternativePerk != null)
                {
                    if (MBRandom.RandomFloat < 0.5f)
                    {
                        hero.HeroDeveloper.AddPerk(item);
                    }
                    else
                    {
                        hero.HeroDeveloper.AddPerk(item.AlternativePerk);
                    }
                }
                else
                {
                    hero.HeroDeveloper.AddPerk(item);
                }
            }
        }
    }
}
