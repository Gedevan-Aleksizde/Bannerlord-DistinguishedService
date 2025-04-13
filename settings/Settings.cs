using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace DistinguishedServiceRedux.settings
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => SubModule.moduleName;

        public override string DisplayName => "Distinguished Service Redux";

        public override string FolderName => SubModule.moduleName;

        public override string FormatType => "json2";

        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyInteger("{=iA1lzksGd}Promotion Cost", 0, 1000, "0", Order = 0, RequireRestart = false, HintText = "{=PWcgmYKdm0}The cost to pay the new hero up-front. Doesn't affect daily payments. Deafult is 0.")]
        public int PromotionCost { get; set; } = 0; // up_front_cost
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyInteger("{=IraZX6JYH}Max. nominations", 1, 128, "0", Order = 1, RequireRestart = false, HintText = "{=xkVDlk1N1V}The maximum number of nominees you are allowed to pick at the end of a battle. Default is 1.")]
        public int MaxNominations { get; set; } = 1; // max_nominations
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyInteger("{=Rw8hDdgtq}Min. Tier", -1, 10, "0", Order = 2, RequireRestart = false, HintText = "{=TDkNix5fda}The minimum tier of unit eligible to become a hero. Set to -1 to only allow units with no further upgrades to be nominated. Default is -1.")]
        public int EligibleTier { get; set; } = 4; // tier_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyInteger("{=NqAfmE3I7}Min. Kills for Infantly Troops", 1, 128, "0", Order = 3, RequireRestart = false, HintText = "{=BLpQfo0f8b}The number of kills threshold to be nominated for infantly. Default is 5.")]
        public int EligibleKillCountInfantry { get; set; } = 5; // inf_kill_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyInteger("{=0owPtnEUD}Min. Kills for Mounted Troops", 1, 128, "0", Order = 4, RequireRestart = false, HintText = "{=CRRwgyzJvq}The number of kills threshold to be nominated for mounted troops. Default is 5.")]
        public int EligibleKillCountCavalry { get; set; } = 5; // cav_kill_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyInteger("{=NNQYYgCN3}Min. Kills for Ranged Troops", 1, 128, "0", Order = 5, RequireRestart = false, HintText = "{=vcXgO4B8mG}The number of kills threshold to be nominated for ranged troops. Default is 5.")]
        public int EligibleKillCountRanged { get; set; } = 5; // ran_kill_threshold
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyFloatingInteger("{=WyI8Nf9yg}Percentile Outperform", 0, 1, "0.000", Order = 6, HintText = "{=aWfPIRIssB}The percentile of kills a unit must exceed to qualify to be nominated. Set to 0 for previous versions' behaviour (only kill thresholds). Default is 0.68.")]
        public float EligiblePercentile { get; set; } = 0.68f;  // outperform_percentile
        [SettingPropertyGroup("{=KAgbRWUNn}Eligibility", GroupOrder = 0)]
        [SettingPropertyBool("{=7C44pRJyT}Ignore Companion Limit", Order = 0, HintText = "{=XoyKSIk9Yo}If enabled, the number of nominations will excess the native companion limit. NOTE: Your companion will randomly LOST if you have more companions than the limit. Default is Disabled.")]
        public bool IgnoreCompanionLimit { get; set; } = false;  // respect_companion_limit, UI改善のため値を反転した
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills", GroupOrder = 1)]
        [SettingPropertyInteger("{=JzTC6uoHk}Skill points", 0, 500, Order = 1, HintText = "{=85WomjlJtB}The number of primary skill point bonus to manually assign to newly-created companion skills. Default is 150.")]
        public int AdditionalSkillPoints { get; set; } = 150; // base_additional_skill_points
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills", GroupOrder = 1)]
        [SettingPropertyInteger("{=FqySMd4Cu}Skill bonus", 0, 10, Order = 2, HintText = "{=KFwunOsf4k}The number of skill bonuses for players to choose for newly-created heroes. Default is 3.")]
        public int NumSkillBonuses { get; set; } = 3; // number_of_skill_bonuses
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills", GroupOrder = 1)]
        [SettingPropertyInteger("{=JEPdqkqB7}Skill rounds", 0, 10, Order = 3, HintText = "{=OfDcCqMwbP}The number of round you can assign skill bonuses during each round gives [base_additional_skill_points/round#] per skill. Default is 1.")]
        public int NumSkillRounds { get; set; } = 1; // number_of_skill_rounds 
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills")]
        [SettingPropertyBool("{=ebrfIjhX9}Randomized skills", Order = 4, HintText = "{=17RpeC0Bd7}If enabled, bonus skill is assigned randomly.")]
        public bool RandomizedSkill { get; set; } = false; // select_skills_randomly
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills", GroupOrder = 1)]
        [SettingPropertyInteger("{=e1gqqB6S9}Skill Bonus per Excess Kills", 0, 100, Order = 5, HintText = "{=x4hncKsa7v}The number of skill points that is awarded to the new companion per kill over the minimum kill threshold. Default is 25.")]
        public int SkillPointsPerExcessKill { get; set; } = 25; // skillpoints_per_excess_kill
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills", GroupOrder = 1)]
        [SettingPropertyInteger("{=WDyB8zjwV}Player's Leadership Skill For Extra 50 Skill Points", 0, 1250, Order = 6, HintText = "{=fegcgJJkot}The number of points of the player's leadership skill point that is required to add 50 extra assignable skill points. Default is 1250.")]
        public int LeadershipPointsPer50ExtraPoints { get; set; } = 1250; // leadership_points_per_50_extra_skill_points
        [SettingPropertyGroup("{=Lwt72jlJJ}Skills", GroupOrder = 1)]
        [SettingPropertyBool("{=RfH84SuAS}Fill In Perks", Order = 7, HintText = "{=UiL3DpJrV1}If enabled, the newly-generated hero's perks are fill in automatically. Default is disabled.")]
        public bool FillInPerks { get; set; } = false; // fill_in_perks
        [SettingPropertyGroup("{=R9S8x4TCU}NPC Parties", GroupOrder = 2)]
        [SettingPropertyInteger("{=3Un7BjHKu}Max. Companions In NPC Parties", 0, 10, Order = 0, HintText = "{=dNmMyhjfih}The maximum allowed number of promoted companions per NPC clan party.")]
        public int MaxPartyCompanionAI { get; set; } = 1;  // max_ai_companions_per_party
        [SettingPropertyGroup("{=R9S8x4TCU}NPC Parties", GroupOrder = 2)]
        [SettingPropertyFloatingInteger("{=MdnnLDQZuO}Chance Of The Promotion In NPC Parties", 0, 1, "0.000", Order = 1, HintText = "{=q2kP1B5pf1}The chance of an NPC lord promoting a properly-tiered unit into a companion after winning a battle. This generates heroes in NPC lords' parties. If you don't have hero death, you might want to set this to zero. Default is 0.001.")]
        public float ChancePromotionAI { get; set; } = 0.001f;  // ai_promotion_chance

        /*
        [SettingPropertyBool("{=608lhFGxj}Remove Wanderers from Tavern", Order = 2, HintText = "{=7kBlaqT7Zg}If enabled, Remove Wanderers from Tavern. Default is disabled.")]
        public bool RemoveTavernCompanion { get; set; } = false;  // remove_tavern_companions // TODO: not implemented
        */
        /*
        [SettingPropertyGroup("{=R9S8x4TCU}NPC Parties")]
        [SettingPropertyBool("{=xMswq98TK}Remove The Companion On Defeat", HintText = "{=Fsn9jOohUy}If enabled the NPC partiy's companion is eliminated after their party is defeated/disbanded. This should be enabled, if you are generating NPC party's companions, as the NPC lords will not gather them back up.")]
        public bool RemoveCompanionOnDefeat { get; set; } = true;  // cull_ai_companions_on_defeat  // TODO: not implemented
        */
        /*
        [SettingPropertyGroup("{=xwNNXlGcq}Companion Capacity")]
        [SettingPropertyInteger("{=WX2tO2HaJ}Companion Slots Bonus Base", 0, 10, Order = 1, HintText = "{=rR0oSObARw}The base value of the number of extra companion slots to add, if Ignore Companion Limit is disables, Set to 0 for native. Default is 3.")]
        public int CompanionSlotsBonusBase { get; set; } = 3;  // bonus_companion_slots_base
        [SettingPropertyGroup("{=xwNNXlGcq}Companion Capacity")]
        [SettingPropertyInteger("{=nbSP8HlmqO}Companion Slots Bonus Per Clan Tier", 0, 10, Order = 1, HintText = "{=ng2TLg4pui}The number of extra companion slots granted per clan tier. Set to 0 for native. This is applied with a targeted Harmony PostFix that should be compatible with other mods that affect this value. Default is 2.")]
        public int CompanionSlotsBonusPerClanTier { get; set; } = 2;  // bonus_companion_slots_per_clan_tier
        */
        /*
        [SettingPropertyGroup("{=ASqPAFgkE}Misc")]
        [SettingPropertyInteger("{=YR3zyglTxz}Extra Lethality", 0, 100, Order = 1, HintText = "{=mv9FDA56bv}Extra chance for a hero with the \"Wanderer\" occupation (not Nobles, or other characters important to the game) to die when they are wounded. If you set the NPC lords' promotion chance higher, you'll want to set this higher, to prevent too many random heroes from being created.")]

        public float ExtraLethalityCompanion { get; set; } = 0;  // companion_extra_lethality  // TODO: not implemented
        */
        [SettingPropertyGroup("{=ASqPAFgkE}Misc", GroupOrder = 3)]
        [SettingPropertyBool("{=v71F1OBfOf}Show Warnings", Order = 0, HintText = "{=lVB5RJoCDm}If disabled, the system warning messages are hidden. Default is enabled.")] // UI改善のため値を反転した
        public bool ShowCautionText { get; set; } = true;  // disable_caution_text
        [SettingPropertyGroup("{=ASqPAFgkE}Misc", GroupOrder = 3)]
        [SettingPropertyBool("{=SzH0vFGPO}Upgrade To Hero", Order = 1, HintText = "{=ZKYkuUYKTQ}If enabled, nomination functionality so that when a unit is upgraded to Eligible Tier they automatically become a hero. Pairs best with high Eligible Tier value, and high lethality. Deafault is disabled.")]
        public bool UpgradeToHero { get; set; } = false; // upgrade_to_hero
    }
}