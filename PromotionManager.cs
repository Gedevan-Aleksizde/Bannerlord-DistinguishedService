/*
 * Author: Thor Tronrud
 * PromotionManager.cs:
 * 
 * Pretty monolothic, and by accretion, not necessity. Acts as a big
 * state object with a lot of static methods providing the utilities
 * used to promote basic troops to companions.
 * 
 * It is fed a list of nominees by the Battle Behaviour class and presents
 * them to the player.
 * 
 * Also includes additional dialogue and supporting methods.
 */

using DistinguishedServiceRedux.ext;
using DistinguishedServiceRedux.settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace DistinguishedServiceRedux
{
    class PromotionManager
    {
        Random rand; //Single random object to use

        //Distinguished Service Specific Lists
        public static PromotionManager __instance = null; //our static instance
        public List<CharacterObject> nominations; //Who's currently nominated for a promotion?
        public List<int> killcounts; //What's their killcount?

        public static bool MyLittleWarbandLoaded = false; //is MLWB loaded? We'll need compatibility adjustments -_-
        /*
        //Settings values
        public bool using_extern_namelist { get; set; } //are we using an external namelist?
        public string extern_namelist { get; set; } //What is it?
        public int max_nominations { get; set; } //How many mooks can be promoted at once?
        public int tier_threshold { get; set; } //Minimum tier (-1 = end)
        public int inf_kill_threshold { get; set; } //Type-specific kill minimums to qualify
        public int cav_kill_threshold { get; set; }
        public int ran_kill_threshold { get; set; }
        public bool fill_perks { get; set; } //fill perks automatically on promotion?
        public float outperform_percentile { get; set; } //What percentile of kills should the nominee lie above?
        public int up_front_cost { get; set; } //Do they cost money to promote?
        public bool respect_companion_limit { get; set; } //Do we care about the game's companion limit?
        public bool ignore_cautions { get; set; } //Do you want to know if something might break?
        private int base_additional_skill_points { get; set; } //How many base skill points do we give these companions?
        private int leadership_points_per_50_extra_skill_points { get; set; } //And a bonus for high leadership
        private int skp_per_excess_kill { get; set; } //How many extra skills points do excess kills grant?
        public bool select_skills_randomly { get; set; } //No player input on skill selections? For games with lots of companions
        public int num_skill_rounds { get; set; } //How many rounds of skill selection do we go through? (Primary, secondary, tertiary, etc...)
        public int num_skill_bonuses { get; set; } //How many specific skills can be selected per round?

        public float ai_promotion_chance { get; set; } //Can AI lords promote troops?
        public int max_ai_comp_per_party { get; set; } //How many do we allow in their party at once?*/


        public PromotionManager()
        {
            //string path = Path.Combine(BasePath.Name, "Modules", "DistinguishedService", "Settings.xml");
            //start with what we know will work
            string path = Path.Combine(TaleWorlds.ModuleManager.ModuleHelper.GetModuleFullPath(SubModule.moduleName), "Settings.xml");
            //check for a settings in the modules folder
            //if it exists, use it instead!
            if (File.Exists(Path.Combine(BasePath.Name, "Modules", SubModule.moduleName, "Settings.xml")))
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Info", "dir").SetTextVariable("DIR", $"Modules/{SubModule.moduleName}").ToString(), Color.FromUint(4282569842U)));
                path = Path.Combine(BasePath.Name, "Modules", SubModule.moduleName, "Settings.xml");
            }
            Settings currentsettings;
            using (Stream stream = (Stream)new FileStream(path, FileMode.Open))
                currentsettings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
            //Set from settings
            /*this.tier_threshold = currentsettings.tier_threshold;
            this.max_nominations = currentsettings.max_nominations;

            this.inf_kill_threshold = currentsettings.inf_kill_threshold;
            this.cav_kill_threshold = currentsettings.cav_kill_threshold;
            this.ran_kill_threshold = currentsettings.ran_kill_threshold;
            this.outperform_percentile = currentsettings.outperform_percentile;
            this.skp_per_excess_kill = currentsettings.skillpoints_per_excess_kill;

            this.up_front_cost = currentsettings.up_front_cost;
            this.fill_perks = currentsettings.fill_in_perks;

            this.respect_companion_limit = currentsettings.respect_companion_limit;
            this.base_additional_skill_points = currentsettings.base_additional_skill_points;
            this.leadership_points_per_50_extra_skill_points = currentsettings.leadership_points_per_50_extra_skill_points;
            this.num_skill_bonuses = currentsettings.number_of_skill_bonuses;
            this.num_skill_rounds = currentsettings.number_of_skill_rounds;
            this.select_skills_randomly = currentsettings.select_skills_randomly;

            this.ai_promotion_chance = currentsettings.ai_promotion_chance;
            this.max_ai_comp_per_party = currentsettings.max_ai_companions_per_party;*/

            rand = new Random();
            nominations = new List<CharacterObject>();
            killcounts = new List<int>();

            // this.using_extern_namelist = currentsettings.NAMES_FROM_EXTERNAL_FILE;
            if (Settings.Instance.NAMES_FROM_EXTERNAL_FILE && NameList.CheckFileExists())
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Info", "usenamelist").ToString(), Color.FromUint(4282569842U)));
            }
            //set other values from settings
            // this.fill_perks = currentsettings.fill_in_perks;

            //Output final mod state to user, set static instance
            InformationManager.DisplayMessage(new(
                GameTexts.FindText("DistServ_Info", "threshold").SetTextVariable("MAX", Settings.Instance.MaxNominations).SetTextVariable("TTHRESH", Settings.Instance.EligibleTier).SetTextVariable("KTHRESH", Settings.Instance.EligibleKillCountInfantry).SetTextVariable("CTHRESH", Settings.Instance.EligibleKillCountCavalry).SetTextVariable("RTHRESH", Settings.Instance.EligibleKillCountRanged).SetTextVariable("PTHRESH", Settings.Instance.EligiblePercentile).ToString(), Color.FromUint(4282569842U)));
            PromotionManager.__instance = this;

            //Display warnings if chosen settings will cause non-player-controlled events
            //e.g. auto perk selection, auto-promotion, ignoring companion limit
            if (Settings.Instance.UpgradeToHero)
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "005autopromotion").SetTextVariable("TTHRESH", Settings.Instance.EligibleTier).ToString(), Colors.Yellow));
            }
            if (Settings.Instance.FillInPerks)
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "006perks").ToString(), Colors.Yellow));
            }
            if (Settings.Instance.IgnoreCompanionLimit)
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "005autopromotion").ToString(), Colors.Yellow));
            }
        }

        /// <summary>
        /// Called when the battle is considered "over"
        /// </summary>
        /// Doing it now sidesteps the UI elements being rendered underneath
        /// the end-of-battle loading screen, which was a pretty insidious bug
        /// The PM instance's nominations and killcounts are populated from the Battle Behaviour
        /// and in this method we go through and make sure the nominations are valid
        public void OnPCBattleEndedResults()
        {
            if (!Settings.Instance.IgnoreCompanionLimit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("DistServ_Info", "nonominations").ToString(), Colors.Blue));
                return;
            }
            List<CharacterObject> charactersNominated = new();
            List<int> killCounts = new();
            if (nominations.Count > 0 && killcounts.Count > 0)
            {
                for (int i = 0; i < nominations.Count; i++)
                {
                    if (MobileParty.MainParty.MemberRoster.Contains(nominations[i]) && nominations[i] != null && nominations[i].HitPoints > 0)
                    {
                        charactersNominated.Add(nominations[i]);
                        killCounts.Add(killcounts[i]);
                    }
                }
            }
            List<CharacterObject> coList;
            // int nominationsMods = this.max_nominations;
            double num = rand.NextDouble();

            //If COs are in the final cut list, order them by killcount, and present them to the player
            //We reference two methods -- genInquiryElements, which creates the little presentation box for the unit,
            //and OnNomineeSelect, which takes each selected nominee and performs the "promotion"
            if (charactersNominated.Count > 0)
            {
                coList = new List<CharacterObject>(charactersNominated).OrderBy<CharacterObject, int>(o => killCounts[charactersNominated.IndexOf(o)]).Reverse().ToList();
                killCounts = new List<int>(killCounts).OrderBy<int, int>(o => killCounts[killCounts.IndexOf(o)]).Reverse().ToList();

                //check if number of possible nominations would put us over the companion limit
                if (!Settings.Instance.IgnoreCompanionLimit && (Settings.Instance.MaxNominations + Clan.PlayerClan.Companions.Count) > Clan.PlayerClan.CompanionLimit)
                {
                    nominationsMods = Clan.PlayerClan.CompanionLimit - Clan.PlayerClan.Companions.Count;
                }
                else
                {
                    nominationsMods = this.max_nominations;
                }

                MBInformationManager.ShowMultiSelectionInquiry(
                    new(
                        GameTexts.FindText("DistServ_inquiry_title", "distinguished").ToString(),
                        GameTexts.FindText("DistServ_inquiry_text", "distinguished").SetTextVariable("N", nominationsMods).ToString(),
                        this.GenInquiryelements(coList, killCounts), true, 0, nominationsMods, GameTexts.FindText("str_done", (string)null).ToString(), GameTexts.FindText("DistServ_inquiry_choice", "Random").ToString(), new Action<List<InquiryElement>>(OnNomineeSelect), (Action<List<InquiryElement>>)null, ""), true);
                return;
            }
        }
        /// <summary>
        /// Take a character object list and killcount, creates a corresponding list of InquiryElements showing the unit's preview and killcount tooltip
        /// </summary>
        /// <param name="cos"></param>
        /// <param name="kills"></param>
        public List<InquiryElement> GenInquiryelements(List<CharacterObject> cos, List<int> kills)
        {
            List<InquiryElement> ies = new();
            for (int q = 0; q < cos.Count; q++)
            {
                if (MobileParty.MainParty.MemberRoster.Contains(cos[q]))
                {
                    ies.Add(new((object)cos[q], cos[q].Name.ToString(), new(CharacterCode.CreateFrom((BasicCharacterObject)cos[q])), true, GameTexts.FindText("DistServ_tip", "killcount").SetTextVariable("COUNT", kills[q]).ToString()));
                }
            }
            return ies;
        }
        /// <summary>
        /// Take the list of selected inquiry elements, and feeds them through the Hero-creation system
        /// </summary>
        /// <param name="ies"></param>
        public void OnNomineeSelect(List<InquiryElement> ies)
        {
            foreach (InquiryElement ie in ies)
            {
                CharacterObject co = (CharacterObject)(ie.Identifier);
                string killhint = ie.Hint.Split(' ')[0];
                if (int.TryParse(killhint, out int killCount))
                {
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_tip", "count").SetTextVariable("NAME", co.Name).SetTextVariable("COUNT", killCount).ToString(), Colors.Red));
                }
                else
                {
                    killCount = -1;
                }
                this.PromoteUnit(co, killCount);
                if (MobileParty.MainParty.MemberRoster.Contains(co))
                {
                    MobileParty.MainParty.MemberRoster.RemoveTroop(co);
                }
            }
        }

        /// <summary>
        /// whether a CO is qualified to be nominated or not
        /// </summary>
        /// <param name="co"></param>
        //Since end-tiers aren't uniform, we have to check if there are any upgrade targets
        //for the default branch
        public static bool IsSoldierQualified(CharacterObject co)
        {
            if (co == null)
            {
                return false;
            }
            if (Settings.Instance.EligibleTier < 0)
            {
                if (co.UpgradeTargets == null || co.UpgradeTargets.Length == 0)
                {
                    return true;
                }
            }
            else
            {
                if (co.Tier >= Settings.Instance.EligibleTier)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set a hero and nerf all their equipment by applying the game's "companion" modifier
        /// </summary>
        /// <param name="hero"></param>
        public void AdjustEquipment(Hero hero)
        {
            Equipment eq = hero.BattleEquipment;

            ItemModifier itemModifier1 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
            ItemModifier itemModifier2 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
            ItemModifier itemModifier3 = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
            for (EquipmentIndex index = EquipmentIndex.WeaponItemBeginSlot; index < EquipmentIndex.NumEquipmentSetSlots; ++index)
            {
                EquipmentElement equipmentElement = eq[index];
                if (equipmentElement.Item != null)
                {
                    if (equipmentElement.Item.ArmorComponent != null)
                        eq[index] = new(equipmentElement.Item, itemModifier1);
                    else if (equipmentElement.Item.HorseComponent != null)
                        eq[index] = new(equipmentElement.Item, itemModifier3);
                    else if (equipmentElement.Item.WeaponComponent != null)
                        eq[index] = new(equipmentElement.Item, itemModifier2);
                }
            }
        }

        //Seventh util function -- add variance to the game's main RPG traits
        //Making a hero with a "reputation" that we could potentially use
        //in the future for inter-companion (and inter-lord) conflict
        public void AddTraitVariance(Hero hero)
        {
            foreach (TraitObject trait in TraitObject.All)
            {
                if (trait == DefaultTraits.Honor || trait == DefaultTraits.Mercy || (trait == DefaultTraits.Generosity || trait == DefaultTraits.Valor) || trait == DefaultTraits.Calculating)
                {
                    int num1 = hero.CharacterObject.GetTraitLevel(trait);
                    float num2 = MBRandom.RandomFloat;
                    //skew towards player's traits
                    if (Hero.MainHero.GetTraitLevel(trait) >= 0.9)
                    {
                        num2 *= 1.2f;
                    }

                    if ((double)num2 < 0.1)
                    {
                        --num1;
                        if (num1 < -1)
                            num1 = -1;
                    }
                    if ((double)num2 > 0.9)
                    {
                        ++num1;
                        if (num1 > 1)
                            num1 = 1;
                    }

                    int num3 = MBMath.ClampInt(num1, trait.MinValue, trait.MaxValue);
                    hero.SetTraitLevel(trait, num3);
                }
            }
        }

        //Here's the primary function for this mod--
        //it takes in a CharacterObject, kill count, and option for player selection of skills
        //and creates a hero from that CO, tweaks that hero's skills and attributes,
        //and adds them to the player's party
        public void PromoteUnit(CharacterObject co, int kills = -1, bool pickSkills = true)
        {
            //Basic check against whether the CO exists
            CharacterObject nco = Game.Current.ObjectManager.GetObject<CharacterObject>(co.StringId);
            co = nco;
            if (co == null)
            {
                return;
            }
            //This set of functions attempts to populate the Hero template we want to mold into the input CharacterObject
            //We first start with more stringent criteria (e.g. first check against the Culture's wanderer templates), 
            //and if all of that has fallen through, we'll just take anything at all that matches the male/female
            CharacterObject wanderer = co.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null));
            if (wanderer == null)
            {
                if (!ignore_cautions)
                    InformationManager.DisplayMessage(new(
                        GameTexts.FindText("DistServ_Caution", "009nowanderer").SetTextVariable("CULTURE", co.Culture.Name).ToString(), Colors.Yellow));
                wanderer = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null));
            }
            //final fallback...
            if (wanderer == null)
            {
                if (!ignore_cautions)
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Warn", "010templete").ToString(), Colors.Red));
                wanderer = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale));
            }
            if (wanderer == null)
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Warn", "011wanderernotfound").ToString(), Colors.Red));
                return;
            }
            Hero specialHero = HeroCreator.CreateSpecialHero(wanderer, (Settlement)null, (Clan)null, (Clan)null, rand.Next(20, 50));
            specialHero.SetName(NameList.DrawNameFormat(co).SetTextVariable("FIRSTNAME", specialHero.FirstName), specialHero.FirstName);
            if (using_extern_namelist)
            {
                TextObject newName = NameList.PullOutNameFromExternalFile();
                if (newName.ToString() != "")
                {
                    specialHero.SetName(NameList.DrawNameFormat(co).SetTextVariable("FIRSTNAME", newName), newName);
                }
            }
            specialHero.Culture = co.Culture;

            //Default formation class seems to be read only, so I could't change it
            specialHero.CharacterObject.DefaultFormationGroup = co.DefaultFormationGroup;


            specialHero.ChangeState(Hero.CharacterStates.Active);
            AddCompanionAction.Apply(Clan.PlayerClan, specialHero);
            AddHeroToPartyAction.Apply(specialHero, MobileParty.MainParty, true);
            CampaignEventDispatcher.Instance.OnHeroCreated(specialHero, false);

            AddTraitVariance(specialHero);
            float adjustedCost = this.up_front_cost;
            //GI gives 30% discount
            if (Hero.MainHero.GetPerkValue(DefaultPerks.Trade.GreatInvestor))
            {
                adjustedCost *= 0.7f;
            }
            //PiP gives 25% discount
            if (Hero.MainHero.GetPerkValue(DefaultPerks.Steward.PaidInPromise))
            {
                adjustedCost *= 0.75f;
            }
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, specialHero, (int)adjustedCost);

            //Has met is now read only. So we're using setHasMet
            specialHero.SetHasMet();

            //special, equipment-formatting try-catch statement
            try
            {
                if (MyLittleWarbandLoaded)
                {
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Info", "compatible").ToString(), Colors.Yellow));
                    specialHero.BattleEquipment.FillFrom(co.FirstBattleEquipment);
                    specialHero.CivilianEquipment.FillFrom(co.FirstCivilianEquipment);
                }
                else
                {
                    specialHero.BattleEquipment.FillFrom(co.RandomBattleEquipment);
                    specialHero.CivilianEquipment.FillFrom(co.RandomCivilianEquipment);
                }
                this.AdjustEquipment(specialHero);
            }
            catch (Exception e)
            {
                if (!ignore_cautions)
                {
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "013equipment").ToString(), Colors.Yellow));
                    Debug.Print(GameTexts.FindText("DistServ_Warn", "internalerr").SetTextVariable("TEXT", e.Message).ToString());
                }
                //leave them naked, alone, and afraid
            }

            specialHero.HeroDeveloper.SetInitialLevel(co.Level);
            Dictionary<SkillObject, int> baselineSkills = new();
            foreach (SkillObject sk in Skills.All)
            {
                baselineSkills[sk] = Math.Min(co.GetSkillValue(sk), 300);
                //specialHero.HeroDeveloper.SetInitialSkillLevel(sk, co.GetSkillValue(sk));
            }
            int currentSkill = 0;
            foreach (SkillObject sk in Skills.All)
            {
                currentSkill = specialHero.GetSkillValue(sk);
                specialHero.HeroDeveloper.ChangeSkillLevel(sk, baselineSkills[sk] - currentSkill);
            }

            int skipToAssign = base_additional_skill_points + 50 * Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) / leadership_points_per_50_extra_skill_points;
            if (kills > 0)
            {
                if (co.IsMounted)
                {
                    skipToAssign += PromotionManager.__instance.skp_per_excess_kill * (kills - PromotionManager.__instance.cav_kill_threshold);
                }
                else if (co.IsRanged)
                {
                    skipToAssign += PromotionManager.__instance.skp_per_excess_kill * (kills - PromotionManager.__instance.ran_kill_threshold);
                }
                else
                {
                    skipToAssign += PromotionManager.__instance.skp_per_excess_kill * (kills - PromotionManager.__instance.inf_kill_threshold);
                }
            }

            TextObject TORound = GameTexts.FindText("DistServ_inquiry_text", "round");
            if (select_skills_randomly)
            {
                for (int i = 1; i <= num_skill_rounds; i++)
                {
                    AssignSkillsRandomly(specialHero, skipToAssign / i, this.num_skill_bonuses);
                }
            }
            else
            {
                for (int i = 1; i <= num_skill_rounds; i++)
                {
                    AssignSkills(specialHero, skipToAssign / i, this.num_skill_bonuses, TORound.SetTextVariable("COUNT", i), co.Name);
                }
            }
            int totToAdd = specialHero.HeroDeveloper.UnspentAttributePoints;
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2, false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 2, false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, 2, false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 2, false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 2, false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, 2, false);
            totToAdd -= 12;

            int toAdd = 0;
            if (totToAdd > 0)
            {
                if (co.IsMounted)
                {
                    toAdd = rand.Next(3);
                    specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, toAdd, false);
                    totToAdd -= toAdd;
                }
                else if (co.IsRanged)
                {
                    toAdd = rand.Next(3);
                    specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, toAdd, false);
                    totToAdd -= toAdd;
                }
                else
                {
                    toAdd = rand.Next(3);
                    specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, toAdd, false);
                    totToAdd -= toAdd;
                }
                List<CharacterAttribute> shuffled_attrs = new List<CharacterAttribute>(Attributes.All);
                Shuffle(shuffled_attrs);
                foreach (CharacterAttribute ca in shuffled_attrs)
                {
                    toAdd = rand.Next(2);
                    specialHero.HeroDeveloper.AddAttribute(ca, toAdd, false);
                    totToAdd -= toAdd;
                    if (totToAdd <= 0)
                        break;
                }
            }

            if (this.fill_perks)
            {
                specialHero.DevelopCharacterStats();
            }
            specialHero.HeroDeveloper.UnspentAttributePoints = 0;


        }

        //Eighth and Ninth Util functions -- Assign skills to the nascent hero
        //either through player selection, or randomly
        //For player selection, we create inquiry elements for each "soft" skill,
        //and allow the player to choose several to give a skill bump to
        //Randomly, we replace player choice with a switch statement
        //
        //We also cap out at 300 to avoid... Problems...
        public void AssignSkills(Hero specialHero, int skPointsAasign, int numSelectedSkills, TextObject fullName, TextObject prev)
        {
            List<InquiryElement> iqes = new();
            List<SkillObject> soList = new() { DefaultSkills.Scouting, DefaultSkills.Crafting, DefaultSkills.Athletics, DefaultSkills.Riding, DefaultSkills.Tactics, DefaultSkills.Roguery, DefaultSkills.Charm, DefaultSkills.Leadership, DefaultSkills.Trade, DefaultSkills.Steward, DefaultSkills.Medicine, DefaultSkills.Engineering };
            foreach (SkillObject so in soList)
            {
                if (specialHero.GetSkillValue(so) < 300)
                    iqes.Add(new($"{so.StringId}_bonus", GameTexts.FindText("DistServ_bonus_title", so.StringId.ToString()).ToString(), null, true, GameTexts.FindText("DistServ_bonus_hint", "text").SetTextVariable("COUNT", skPointsAasign).SetTextVariable("SKILLNAME", so.Name.ToString()).ToString()));
            }
            MultiSelectionInquiryData msid = new(
                GameTexts.FindText("DistServ_inquiry_title", "select").SetTextVariable("FULLNAME", fullName).ToString(),
                GameTexts.FindText("DistServ_inquiry_text", "select").SetTextVariable("NAME", specialHero.Name).SetTextVariable("PREV", prev).ToString(),
                iqes,
                true,
                numSelectedSkills,
                numSelectedSkills,
                GameTexts.FindText("DistServ_inquiry_choice", "Accept").ToString(),
                GameTexts.FindText("DistServ_inquiry_choice", "Refuse").ToString(),
                (Action<List<InquiryElement>>)((List<InquiryElement> ies) =>
            {
                int diff = 0;
                foreach (InquiryElement ie in ies)
                {
                    foreach (SkillObject so in soList)
                    {
                        if ($"{so.StringId}_bonus" == (string)ie.Identifier)
                        {
                            try
                            {
                                diff = 300 - specialHero.GetSkillValue(so);
                                specialHero.HeroDeveloper.ChangeSkillLevel(so, Math.Min(skPointsAasign, diff));
                            }
                            catch { }
                        }

                    }
                    try
                    {
                        specialHero.CheckInitialLevel();
                    }
                    catch (Exception e)
                    {
                        if (!ignore_cautions)
                            InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "014levelerro").ToString(), Colors.Yellow));
                    }
                }
            }),
            (Action<List<InquiryElement>>)null);
            MBInformationManager.ShowMultiSelectionInquiry(msid, true);
        }
        public void AssignSkillsRandomly(Hero specialHero, int skPointsAasign, int numSelectedSkills)
        {
            List<SkillObject> soList = new() { DefaultSkills.Scouting, DefaultSkills.Crafting, DefaultSkills.Athletics, DefaultSkills.Riding, DefaultSkills.Tactics, DefaultSkills.Roguery, DefaultSkills.Charm, DefaultSkills.Leadership, DefaultSkills.Trade, DefaultSkills.Steward, DefaultSkills.Medicine, DefaultSkills.Engineering };
            for (int i = 0; i < numSelectedSkills; i++)
            {
                int rv = (int)(MBRandom.RandomFloat * soList.Count);
                try
                {
                    specialHero.HeroDeveloper.ChangeSkillLevel(soList[rv], skPointsAasign);
                }
                catch { }
                break;
            }
            foreach (SkillObject sk in Skills.All)
            {
                int diff = 300 - specialHero.GetSkillValue(sk);
                if (diff < 0)
                {
                    specialHero.HeroDeveloper.ChangeSkillLevel(sk, diff); //subtract
                }
            }
            try
            {
                specialHero.CheckInitialLevel();
            }
            catch (Exception e)
            {
                if (!ignore_cautions)
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "014levelerro").ToString(), Colors.Yellow));
            }
        }

        /// <summary>
        /// shuffle function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        //Setting specific functions, added as event triggers

        //Recruit to hero -- if you recruit a qualified unit, turn them into a hero immediately
        public void RecruitAsHero(CharacterObject troop, int amount)
        {
            if (!IsSoldierQualified(troop) || !MobileParty.MainParty.MemberRoster.Contains(troop))
                return;
            for (int i = 0; i < amount; i++)
            {
                if (this.respect_companion_limit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
                { //stop giving companions if over companion limit and respecting it
                    MobileParty.MainParty.MemberRoster.RemoveTroop(troop, i);
                    return;
                }
                PromotionManager.__instance.PromoteUnit(troop);
            }
            MobileParty.MainParty.MemberRoster.RemoveTroop(troop, amount);
        }
        //And finally, for upgraded units
        public void UpgradeToHero(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
        {
            if (!IsSoldierQualified(upgradeToTroop))
                return;
            for (int i = 0; i < number; i++)
            {
                if (this.respect_companion_limit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
                { //stop giving companions if over companion limit and respecting it
                    MobileParty.MainParty.MemberRoster.RemoveTroop(upgradeToTroop, i);
                    return;
                }
                PromotionManager.__instance.PromoteUnit(upgradeToTroop);
            }
            MobileParty.MainParty.MemberRoster.RemoveTroop(upgradeToTroop, number);
        }


        //Console commands to both test out functionality, and allow players to set up

        //their own playthrough as they want:
        [CommandLineFunctionality.CommandLineArgumentFunction("uplift_soldier", "dservice")]
        public static string NewGuyCheat(List<string> strings)
        {
            int tierthresh = -1;
            if (!CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
                return "Usage: uplift_soldier [tier threshold = 0]";
            if (!int.TryParse(strings[0], out tierthresh))
                tierthresh = 0;
            List<CharacterObject> cos = new();
            List<int> fauxKills = new();
            foreach (CharacterObject co in MobileParty.MainParty.MemberRoster.ToFlattenedRoster().Troops)
            {
                if (co.IsHero || co.Tier < tierthresh)
                    continue;
                cos.Add(co);
                fauxKills.Add(1337);
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(GameTexts.FindText("DistServ_inquiry_title", "console").ToString(), GameTexts.FindText("DistServ_inquiry_text", "console").ToString(), PromotionManager.__instance.GenInquiryelements(cos, fauxKills), true, 0, 1, GameTexts.FindText("str_done", (string)null).ToString(), GameTexts.FindText("DistServ_inquiry_choice", "Random").ToString(), new Action<List<InquiryElement>>(PromotionManager.__instance.OnNomineeSelect), (Action<List<InquiryElement>>)null, ""), true);

            return "Dialog Generated";
        }


        //Finally, option for AI promotions -- add them to a party

        //First, we scan concluded map events. If they were a battle,
        //we spoof a determination by rolling a random number between 0-1
        //If it hits, we shove one of their qualifying troops through the
        //hero generation system
        public void MapEventEnded(MapEvent me)
        {
            //while this kinda feels like cheating, it's in C# so it's not like
            //performance is the goal anyway
            try
            {
                //only care about decisive field battles
                if (!(me.HasWinner))
                    return;
                Random r = new();  // TODO: ここは MBRandom ではないのか

                //look at winning side
                foreach (MapEventParty p in me.PartiesOnSide(me.WinningSide))
                {
                    if (p == null || p.Party == PartyBase.MainParty || p.Party.LeaderHero?.Clan == Clan.PlayerClan)
                    {
                        continue;
                    }
                    if (p.Party.LeaderHero != null)
                    {
                        if (r.NextDouble() > this.ai_promotion_chance)
                            continue;
                        List<CharacterObject> characterObjects = p.Troops.Troops.ToList();
                        if (characterObjects == null)
                        {
                            continue;
                        }
                        this.Shuffle(characterObjects);
                        List<CharacterObject> qualified = new();
                        int companionsInParty = 0;
                        foreach (CharacterObject co in characterObjects)
                        {
                            if (co.IsHero && co.HeroObject.Occupation == Occupation.Wanderer)
                            {
                                companionsInParty++;
                            }
                            if (!(co == null) && !co.IsHero && co.IsSoldier && PromotionManager.IsSoldierQualified(co))
                            {
                                qualified.Add(co);
                            }
                        }
                        if (companionsInParty < max_ai_comp_per_party && qualified.Count > 0)
                        {
                            this.PromoteToParty(qualified[0], p.Party.MobileParty);
                        }
                    }
                }
            }
            catch { }
        }
        public void PromoteToParty(CharacterObject co, MobileParty party)
        {
            if (co == null || party == null)
            {
                return;
            }
            Hero partyLeader = party?.LeaderHero;
            if (partyLeader == null)
            {
                return; //needs seed hero
            }

            CharacterObject wanderer = co.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null));
            if (wanderer == null)
            {
                wanderer = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == co.IsFemale && x.CivilianEquipments != null));
            }
            if (wanderer == null)
            {
                //big fuck-up here! No eligible wanderers at all
                return;
            }
            Hero specialHero = HeroCreator.CreateSpecialHero(wanderer, (Settlement)null, (Clan)null, (Clan)null, rand.Next(20, 50));
            specialHero.SetName(NameList.DrawNameFormat(co).SetTextVariable("FIRSTNAME", specialHero.FirstName), specialHero.FirstName);
            specialHero.Culture = co.Culture;
            specialHero.ChangeState(Hero.CharacterStates.Active);
            AddHeroToPartyAction.Apply(specialHero, party, true);
            CampaignEventDispatcher.Instance.OnHeroCreated(specialHero, false);
            AddTraitVariance(specialHero);
            GiveGoldAction.ApplyBetweenCharacters(partyLeader, specialHero, this.up_front_cost, true);
            //specialHero.HasMet = false; //Has met seems to be read only in 1.1.0. There is SetHasMet() but it does not take any parameters, sooo idk how to set it to false
            //special, equipment-formatting try-catch statement
            try
            {
                specialHero.BattleEquipment.FillFrom(co.FirstBattleEquipment);//co.RandomBattleEquipment);
                specialHero.CivilianEquipment.FillFrom(co.FirstCivilianEquipment);// co.RandomCivilianEquipment);
                this.AdjustEquipment(specialHero);
            }
            catch (Exception e)
            {
                if (!ignore_cautions)
                {
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "015equipincomp").ToString(), Colors.Yellow));
                    Debug.Print("Equipment format issue, providing default equipment instead! Exception details:\n" + e.Message);
                }
                //leave them naked, alone, and afraid
            }

            if (co.IsMounted)
            {
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2 + rand.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 1 + rand.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 4 + rand.Next(3), false);
            }
            else if (co.IsRanged)
            {
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2 + rand.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 4 + rand.Next(3), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 1 + rand.Next(2), false);
            }
            else
            {
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 3 + rand.Next(3), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 2 + rand.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 2 + rand.Next(2), false);
            }
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, 1 + rand.Next(3), false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, 1 + rand.Next(3), false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 1 + rand.Next(3), false);
            foreach (SkillObject sk in Skills.All)
            {
                specialHero.HeroDeveloper.ChangeSkillLevel(sk, co.GetSkillValue(sk), false);
            }
            List<SkillObject> shuffledSkills = new List<SkillObject>(Skills.All);
            Shuffle(shuffledSkills);
            int skipToAssign = base_additional_skill_points + 50 * partyLeader.GetSkillValue(DefaultSkills.Leadership) / leadership_points_per_50_extra_skill_points;
            int bonus = 0;
            foreach (SkillObject sk in shuffledSkills)
            {
                if (sk == DefaultSkills.OneHanded || sk == DefaultSkills.TwoHanded || sk == DefaultSkills.Polearm || sk == DefaultSkills.Bow || sk == DefaultSkills.Crossbow || sk == DefaultSkills.Throwing)
                {
                    bonus = rand.Next(10) + rand.Next(15);
                }
                else
                {
                    bonus = rand.Next(10) + rand.Next(15) + rand.Next(25);
                }
                skipToAssign -= bonus;
                if (skipToAssign < 0)
                    bonus += skipToAssign;
                try
                {
                    specialHero.HeroDeveloper.ChangeSkillLevel(sk, bonus, false);
                }
                catch (Exception e)
                {
                }

                //specialHero.HeroDeveloper.UnspentFocusPoints += specialHero.Level;

                if (skipToAssign <= 0)
                    break;
            }
            try
            {
                specialHero.CheckInitialLevel();
            }
            catch (Exception e)
            {
                //nothing, just prevent random crashes from out of nowhere
            }
            specialHero.GetOneAvailablePerkForEachPerkPair();
            specialHero.DevelopCharacterStats();
        }

        /*
         * Second half of this file concerns interactions with companions
         * that I felt the game was lacking.
         * These include options to change their name, move companions between parties, etc
         */

        //Add the dialog options to the game
        public static void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            //name change
            campaignGameStarter.AddPlayerLine("companion_change_name_start", "hero_main_options", "companion_change_name_confirm", GameTexts.FindText("DistServ_dialog", "rename").ToString(), new(GetNamechangecondition), new(GetNamechanceconsequence), 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
            campaignGameStarter.AddDialogLine("companion_change_name_confirm", "companion_change_name_confirm", "hero_main_options", GameTexts.FindText("DistServ_dialog", "answer").ToString(), (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);

            //give companion to party
            campaignGameStarter.AddPlayerLine("companion_transfer_start", "hero_main_options", "companion_transfer_confirm", GameTexts.FindText("DistServ_dialog", "transfer").ToString(), new(GetGiveCompToClanPartyCondition), new(GetGiveCompToClanPartyConsequence), 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
            campaignGameStarter.AddDialogLine("companion_transfer_confirm", "companion_transfer_confirm", "hero_main_options", GameTexts.FindText("DistServ_dialog", "answer").ToString(), (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);

            //take companion back from party
            campaignGameStarter.AddPlayerLine("companion_takeback_start", "hero_main_options", "companion_takeback_confirm", GameTexts.FindText("DistServ_dialog", "takeback").ToString(), new(GetTakeCompFromClanPartyCondition), new(GetTakeCompFromClanPartyConsequence), 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
            campaignGameStarter.AddDialogLine("companion_takeback_confirm", "companion_takeback_confirm", "hero_main_options", GameTexts.FindText("DistServ_dialog", "answer").ToString(), (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);

            //poach a companion from a defeated party
            campaignGameStarter.AddPlayerLine("enemy_comp_recruit_1", "defeated_lord_answer", "companion_poach_confirm", GameTexts.FindText("DistServ_dialog", "poach").ToString(), new(PromotionManager.GetCapturedAIWandererCondition), (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null); //new ConversationSentence.OnClickableConditionDelegate(CanConvertWanderer)
            campaignGameStarter.AddDialogLine("enemy_comp_recruit_2", "companion_poach_confirm", "close_window", "{RECRUIT_RESPONSE}", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);

        }

        //Name change logic --
        //A condition for whether the option will appear
        //A consequence that prompts the player for a new name
        //And a result that sets the companion's new name
        private static bool GetNamechangecondition()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.IsPlayerCompanion;
        }
        private static void GetNamechanceconsequence()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData("Create a new name: ", string.Empty, true, false, GameTexts.FindText("str_done", (string)null).ToString(), (string)null, new Action<string>(PromotionManager.ChangeHeroName), (Action)null, false), false);

        }
        private static void ChangeHeroName(string s)
        {
            Hero.OneToOneConversationHero.SetName(new TextObject(s), new TextObject(s));
        }


        //Companion transferrence logic --
        //Can you ask a companion to take other companions into their party?
        //Select who goes
        //Explicitly move them to the new party
        private static bool GetGiveCompToClanPartyCondition()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && (Hero.OneToOneConversationHero.IsPartyLeader);
        }
        private static void GetGiveCompToClanPartyConsequence()
        {
            MBInformationManager.ShowMultiSelectionInquiry(new(GameTexts.FindText("DistServ_inquiry_title", "trsfhero").ToString(), GameTexts.FindText("DistServ_inquiry_text", "trsfhero").SetTextVariable("HERO", Hero.OneToOneConversationHero.Name).ToString(), PromotionManager.GenTransferList(PromotionManager.GetPlayerPartyHeroCOs()), true, 0, PartyBase.MainParty.MemberRoster.Count, GameTexts.FindText("str_done", (string)null).ToString(), GameTexts.FindText("DistServ_inquiry_choice", "Nobody").ToString(), new Action<List<InquiryElement>>(PromotionManager.TransferCompsToConversationParty), (Action<List<InquiryElement>>)null, ""), true);
        }
        private static void TransferCompsToConversationParty(List<InquiryElement> ies)
        {
            MobileParty conv = Hero.OneToOneConversationHero.PartyBelongedTo;
            foreach (InquiryElement ie in ies)
            {
                CharacterObject co = (CharacterObject)(ie.Identifier);
                Hero h = co.HeroObject;

                AddHeroToPartyAction.Apply(h, conv, true);
            }
        }
        //Util function to create a list of heros in the player's party
        private static List<CharacterObject> GetPlayerPartyHeroCOs()
        {
            List<CharacterObject> hs = new List<CharacterObject>();
            foreach (TroopRosterElement tre in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                CharacterObject co = tre.Character;
                if (!co.IsHero || co.IsPlayerCharacter)
                    continue;
                hs.Add(co);
            }
            return hs;
        }

        //Take heros from your clan's party logic --
        //Is this a party leader of your clan you're talking to
        //Select who to steal from their party
        //Explicitly transfer
        private static bool GetTakeCompFromClanPartyCondition()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && (Hero.OneToOneConversationHero.IsPartyLeader);
        }
        private static void GetTakeCompFromClanPartyConsequence()
        {
            MBInformationManager.ShowMultiSelectionInquiry(new(GameTexts.FindText("DistServ_inquiry_title", "trsfhero").ToString(), GameTexts.FindText("DistServ_inquiry_text", "trsfhero").SetTextVariable("HERO", Hero.OneToOneConversationHero.Name).ToString(), PromotionManager.GenTransferList(PromotionManager.GetConversationPartyHeros(), false), true, 0, PartyBase.MainParty.MemberRoster.Count, GameTexts.FindText("str_done", (string)null).ToString(), GameTexts.FindText("DistServ_inquiry_chice", "Nobody").ToString(), new Action<List<InquiryElement>>(PromotionManager.TransferCompsFromConversationParty), (Action<List<InquiryElement>>)null, ""), true);
        }
        private static void TransferCompsFromConversationParty(List<InquiryElement> ies)
        {
            foreach (InquiryElement ie in ies)
            {
                CharacterObject co = (CharacterObject)(ie.Identifier);
                Hero h = co.HeroObject;
                AddHeroToPartyAction.Apply(h, MobileParty.MainParty, true);
            }
        }
        //Util function -- Generates a list of InquiryElements from a list of CharacterObjects 
        public static List<InquiryElement> GenTransferList(List<CharacterObject> cos, bool isMainPartyRequired = true)
        {
            List<InquiryElement> _ies = new List<InquiryElement>();

            foreach (CharacterObject _co in cos)
            {
                if ((!isMainPartyRequired) || (isMainPartyRequired && MobileParty.MainParty.MemberRoster.Contains(_co)))
                {
                    _ies.Add(new((object)_co, _co.Name.ToString(), new(CharacterCode.CreateFrom((BasicCharacterObject)_co)), true, " kills"));
                }
            }

            return _ies;

        }
        //Util function -- Gets list of heros in the party of the hero you are conversing with
        private static List<CharacterObject> GetConversationPartyHeros()
        {
            PartyBase convparty = Hero.OneToOneConversationHero.PartyBelongedTo.Party;
            List<CharacterObject> hs = new();
            foreach (TroopRosterElement tre in convparty.MemberRoster.GetTroopRoster())
            {
                CharacterObject co = tre.Character;
                if (!co.IsHero || co.HeroObject == Hero.OneToOneConversationHero)
                {
                    continue;
                }
                hs.Add(co);
            }
            return hs;
        }

        //Condition for whether a captured enemy wanderer will consider switching sides if
        //your "values" align more closely to theirs
        private static bool GetCapturedAIWandererCondition()
        {
            if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan != Clan.PlayerClan && Hero.OneToOneConversationHero.Occupation == Occupation.Wanderer)
            {
                //get potential relation with player
                float points = 0.0f;
                float maxContrib = 0; //track highest contribution to points
                float minContrib = 0; //track lowest, to set rejection text
                int maxOne = 0;
                int minOne = 0;
                float temp = 0;
                //calculating will like player for being better, not-calculating just vibesss
                temp = MathF.Max(0.25f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Calculating), 0.0f);
                if (temp > maxContrib)
                {
                    maxOne = 1;
                }
                else if (temp < minContrib)
                {
                    minOne = 1;
                }
                //impulsiveness
                temp = MathF.Max(-0.25f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Calculating), 0.0f);
                if (temp > maxContrib)
                {
                    maxOne = 5;
                }
                else if (temp < minContrib)
                {
                    minOne = 5;
                }
                points += temp;
                //honorable AI won't like idea of joining up with player
                //dishonorable will prefer it
                temp = -0.5f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Honor);
                if (-temp > maxContrib)
                {
                    maxOne = 2;
                }
                else if (temp < minContrib)
                {
                    minOne = 2;
                }
                points += temp;
                //Risk-taking AI will want to join
                //risk-averse won't
                temp = 0.5f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Generosity);
                if (temp > maxContrib)
                {
                    maxOne = 3;
                }
                else if (temp < minContrib)
                {
                    minOne = 3;
                }
                points += temp;
                //Valourous AI will think about how glorious it could be
                temp = 0.25f * MBRandom.RandomFloatRanged(0.1f, 0.5f) * Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Valor);
                if (temp > maxContrib)
                {
                    maxOne = 4;
                }
                else if (temp < minContrib)
                {
                    minOne = 4;
                }
                points += temp;
                bool success = MBRandom.RandomFloat < points;
                if (success)
                {
                    if (1 <= maxOne && maxOne <= 5)
                    {
                        MBTextManager.SetTextVariable("RECRUIT_RESPONSE", GameTexts.FindText("DistServ_comment_accept", $"00{maxOne}"));
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("RECRUIT_RESPONSE", GameTexts.FindText("DistServ_comment_accept", "000"));
                    }
                    Hero.OneToOneConversationHero.ChangeState(Hero.CharacterStates.Active);
                    AddCompanionAction.Apply(Clan.PlayerClan, Hero.OneToOneConversationHero);
                    AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty, true);

                    return true;
                }
                else
                {
                    if (1 <= maxOne && maxOne <= 4)
                    {
                        MBTextManager.SetTextVariable("RECRUIT_RESPONSE", GameTexts.FindText("DistServ_comment_reject", $"00{maxOne}"));
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("RECRUIT_RESPONSE", GameTexts.FindText("DistServ_comment_reject", "000"));
                    }
                    return true;
                }
            }
            return false;
        }

    }
}
