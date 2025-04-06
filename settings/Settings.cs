using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace DistinguishedServiceRedux.settings
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "DistinguishedServiceRedux";

        public override string DisplayName => "Distinguished Service";

        public override string FolderName => "DistinguishedServiceRedux";

        public override string FormatType => "json2";

        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyInteger("Promotion Cost", 0, 1000, "0", Order = 0, RequireRestart = false, HintText = "The cost to pay the new hero up-front. Doesn't affect daily payments. Deafult is 0.")]
        public int PromotionCost { get; set; } = 0; // up_front_cost
        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyInteger("Max nominations", 1, 128, "0", Order = 2, RequireRestart = false, HintText = "The maximum number of nominees you are allowed to pick at the end of a battle. Default is 1.")]
        public int MaxNominations { get; set; } = 1; // max_nominations
        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyInteger("Eligible Tier", -1, 10, "0", Order = 3, RequireRestart = false, HintText = "The minimum tier of unit eligible to become a hero. Set to -1 to only allow units with no further upgrades to be nominated. Default is -1.")]
        public int EligibleTier { get; set; } = 4; // tier_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyInteger("Min Kills for a infantly", 1, 128, "0", Order = 4, RequireRestart = false, HintText = "The number of kills threshold to be nominated for a infantly. Default is 5.")]
        public int EligibleKillCountInfantry { get; set; } = 5; // inf_kill_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyInteger("Min Kills for a cavalry", 1, 128, "0", Order = 5, RequireRestart = false, HintText = "The number of kills threshold to be nominated for a cavalry. Default is 5.")]
        public int EligibleKillCountCavalry { get; set; } = 5; // cav_kill_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyInteger("Min Kills for a ranged", 1, 128, "0", Order = 6, RequireRestart = false, HintText = "The number of kills threshold to be nominated for a ranged tropps. Default is 5.")]
        public int EligibleKillCountRanged { get; set; } = 5; // ran_kill_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}DistServMCMEligibility")]
        [SettingPropertyFloatingInteger("Percentile Outperform", 0, 1, "0.000", Order = 7, HintText = "The percentile of kills a unit must exceed to qualify to be nominated. Set to 0 for previous versions' behaviour (only kill thresholds)")]
        public float EligiblePercentile { get; set; } = 0.68f;  // outperform_percentile

        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyInteger("Skill points", 0, 500, Order = 0, HintText = "The number of primary skill point bonus to manually assign to newly-created companion skills. Default is 150.")]
        public int AdditionalSkillPoints { get; set; } = 150; // base_additional_skill_points
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyInteger("Skill bonus", 0, 10, Order = 1, HintText = "The number of skill bonuses for players to choose for newly-created heroes. Default is 3.")]
        public int NumSkillBonuses { get; set; } = 3; // number_of_skill_bonuses
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyInteger("Skill rounds", 0, 10, Order = 2, HintText = "The number of round you can assign skill bonuses during each round gives [base_additional_skill_points/round#] per skill. Default is 2.")]
        public int NumSkillRounds { get; set; } = 2; // number_of_skill_rounds 
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyBool("Randomized skill", IsToggle = true, Order = 3, HintText = "Should skills to invest points into be selected for you?")]
        public bool RandomizedSkill { get; set; } = false; // select_skills_randomly
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyInteger("Skill Bonus per Excess Kills", 0, 100, Order = 4, HintText = "The number of skill points that is awarded to the new companion per kill over the minimum kill threshold. Default is 25.")]
        public int skillpoints_per_excess_kill { get; set; } = 25; // skillpoints_per_excess_kill
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyInteger("Player's Leadership Skill For Extra 50 Skill Points", 0, 1250, Order = 5, HintText = "The number of points of the player's leadership skill point that is required to add 50 extra assignable skill points. Default is 1250.")]
        public int LeadershipPointsPer50ExtraPoints { get; set; } = 1250; // leadership_points_per_50_extra_skill_points
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyBool("Fill in perks", IsToggle = true, Order = 6, HintText = "If enabled, the newly-generated hero's perks are fill in automatically. Default is disabled.")]
        public bool FillInPerks { get; set; } = false; // fill_in_perks
        [SettingPropertyGroup("{=R9S8x4TCU}NPC Parties")]
        [SettingPropertyInteger("Max Companions In NPC Parties", 0, 10, Order = 1, HintText = "The maximum allowed number of ai-generated companions per ai-clan party.")]
        public int MaxPartyCompanionAI { get; set; } = 1;  // max_ai_companions_per_party
        [SettingPropertyBool("Remove Companions from Tavern", IsToggle = true, Order = 2, HintText = "If enabled, ... Default is disabled.")]
        public bool RemoveTavernCompanion { get; set; } = false;  // remove_tavern_companions
        [SettingPropertyBool("Remove The Companion On Defeat", HintText = "If enabled the ai-generated companion is eliminated after their party is defeated/disbanded. This should be set to \"true\", if you are generating ai companions, as the AI lords will not gather them back up.")]
        public bool RemoveCompanionOnDefeat { get; set; } = true;  // cull_ai_companions_on_defeat
        [SettingPropertyBool("Upgrade To Hero", IsToggle = true, Order = 1, HintText = "If enabled, nomination functionality so that when a unit is upgraded to Eligible Tier they automatically become a hero. Pairs best with high Eligible Tier value, and high lethality. Deafault is disabled.")]
        public bool UpgradeToHero { get; set; } = false; // upgrade_to_hero
        [SettingPropertyGroup("{=xwNNXlGcq}Companion Capacity")]
        [SettingPropertyBool("Ignore Companion Limit", HintText = "If enabled, the number of nominations is NOT constrained by the native companion limit. Default is enabled.")]
        public bool IgnoreCompanionLimit { get; set; } = true;  // respect_companion_limit, UI改善のため値を反転した
        [SettingPropertyGroup("{=xwNNXlGcq}Companion Capacity")]
        [SettingPropertyInteger("Companion Slots Bonus Base", 0, 10, Order = 1, HintText = "The base value of the number of extra companion slots to add, if Ignore Companion Limit is disables, Set to 0 for native. Default is 3.")]
        public int CompanionSlotsBonusBase { get; set; } = 3;  // bonus_companion_slots_base
        [SettingPropertyGroup("{=xwNNXlGcq}Companion Capacity")]
        [SettingPropertyInteger("Companion Slots Bonus Per Clan Tier", 0, 10, Order = 1, HintText = "The number of extra companion slots granted per clan tier. Set to 0 for native. This is applied with a targeted Harmony PostFix that should be compatible with other mods that affect this value. Default is 2.")]
        public int CompanionSlotsBonusPerClanTier { get; set; } = 2;  // bonus_companion_slots_per_clan_tier
        [SettingPropertyInteger("Extra Letality", 0, 100, Order = 1, HintText = "Extra chance for a hero with the \"Wanderer\" occupation (not Nobles, or other characters important to the game) to die when they are wounded. If you set the AI lords' promotion chance higher, you'll want to set this higher, to prevent too many random heroes from being created.")]
        public float ExtraLethalityCompanion { get; set; } = 0;  // companion_extra_lethality
        [SettingPropertyGroup("{=R9S8x4TCU}NPC Parties")]
        [SettingPropertyFloatingInteger("Chance Of The Promotion In NPC Parties", 0, 1, "0.000", Order = 1, HintText = "The chance of an AI lord promoting a properly-tiered unit into a companion after winning a battle. This generates heroes in AI lords' parties. If you don't have hero death, you might want to set this to zero.")]
        public float ChancePromotionAI { get; set; } = 0.001f;  // ai_promotion_chance
        [SettingPropertyGroup("{=ASqPAFgkE}Misc")]
        [SettingPropertyBool("Show Warnings", HintText = "If disabled, the system warning messages are hidden. Default is enabled.")] // UI改善のため値を反転した
        public bool ShowCautionText { get; set; } = true;  // disable_caution_text
        public bool NAMES_FROM_EXTERNAL_FILE { get; set; }
        public string EXTERNAL_NAME_FILE { get; set; } = "external_namelist.txt";
    }
}

namespace DistinguishedServiceRedux
{
    /*
    public class Settings
    {
        public int up_front_cost { get; set; }
        public int tier_threshold { get; set; }
        public int base_additional_skill_points { get; set; }
        public int leadership_points_per_50_extra_skill_points { get; set; }
        public int inf_kill_threshold { get; set; }
        public int cav_kill_threshold { get; set; }
        public int ran_kill_threshold { get; set; }
        public float outperform_percentile { get; set; }
        public int max_nominations { get; set; }
        public bool upgrade_to_hero { get; set; }
        public bool fill_in_perks { get; set; }
        public bool respect_companion_limit { get; set; }
        public int bonus_companion_slots_base { get; set; }
        public int bonus_companion_slots_per_clan_tier { get; set; }

        public float companion_extra_lethality { get; set; }
        public float ai_promotion_chance { get; set; }
        public int number_of_skill_bonuses { get; set; }

        public int number_of_skill_rounds { get; set; }

        public bool select_skills_randomly { get; set; }

        public bool remove_tavern_companions { get; set; }

        public bool cull_ai_companions_on_defeat { get; set; }
        public bool disable_caution_text { get; set; }

        public int skillpoints_per_excess_kill { get; set; }
        public int max_ai_companions_per_party { get; set; }

        public bool NAMES_FROM_EXTERNAL_FILE { get; set; } = false;
        public string EXTERNAL_NAME_FILE { get; set; } = "external_namelist.txt";

        public Settings()
        {
            this.up_front_cost = 0;
            this.tier_threshold = 4;
            this.base_additional_skill_points = 150;
            this.leadership_points_per_50_extra_skill_points = 100;
            this.number_of_skill_bonuses = 3;
            this.inf_kill_threshold = 5;
            this.cav_kill_threshold = 5;
            this.ran_kill_threshold = 5;
            this.outperform_percentile = 0.68f;
            this.max_nominations = 1;
            this.upgrade_to_hero = false;
            this.fill_in_perks = false;
            this.respect_companion_limit = false;
            this.bonus_companion_slots_base = 3;
            this.bonus_companion_slots_per_clan_tier = 2;
            this.companion_extra_lethality = 0;
            this.ai_promotion_chance = 0.001f;
            this.remove_tavern_companions = true;
            this.cull_ai_companions_on_defeat = true;
            this.disable_caution_text = false;
            this.skillpoints_per_excess_kill = 25;
            this.NAMES_FROM_EXTERNAL_FILE = false;
            this.EXTERNAL_NAME_FILE = "external_namelist.txt";
            this.max_ai_companions_per_party = 1;
            this.number_of_skill_rounds = 2;
            this.select_skills_randomly = false;
        }
    }*/
}
