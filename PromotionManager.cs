/*
 * Original Author: Thor Tronrud
 * TODO: remove many less-informative comments
 * TODO: refactor redundant functions
 */

using DistinguishedServiceRedux.ext;
using DistinguishedServiceRedux.settings;
using Helpers;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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
        private Random rng;
        public static PromotionManager? __instance = null; // TODO: really needed?
        public List<CharacterObject> nominations;
        public List<int> killcounts;

        public static bool MyLittleWarbandLoaded = false; // TODO: really should be static?


        public PromotionManager()
        {
            this.rng = new MersenneTwister(); // Bannerlord default rng is native XoRShift... // TODO: I should use faster one, e.g., XoShiro256
            this.nominations = new List<CharacterObject>();
            this.killcounts = new List<int>();

            if (NameList.IsFileExists())
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Info", "usenamelist").ToString(), Colors.White));
            }

            //Output final mod state to user, set static instance
            InformationManager.DisplayMessage(new(
                GameTexts.FindText("DistServ_Info", "threshold").SetTextVariable("MAX", Settings.Instance.MaxNominations).SetTextVariable("TTHRESH", Settings.Instance.EligibleTier).SetTextVariable("KTHRESH", Settings.Instance.EligibleKillCountInfantry).SetTextVariable("CTHRESH", Settings.Instance.EligibleKillCountCavalry).SetTextVariable("RTHRESH", Settings.Instance.EligibleKillCountRanged).SetTextVariable("PTHRESH", Settings.Instance.EligiblePercentile).ToString(), Colors.White));
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
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("DistServ_Info", "003companionlimit").ToString(), Colors.White));
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


            //If COs are in the final cut list, order them by killcount, and present them to the player
            //We reference two methods -- genInquiryElements, which creates the little presentation box for the unit,
            //and OnNomineeSelect, which takes each selected nominee and performs the "promotion"

            if (charactersNominated.Count > 0)
            {
                coList = charactersNominated.OrderBy<CharacterObject, int>(o => killCounts[charactersNominated.IndexOf(o)]).Reverse().ToList();
                killCounts = killCounts.OrderBy<int, int>(o => killCounts[killCounts.IndexOf(o)]).Reverse().ToList();
                int nominations = Math.Min(charactersNominated.Count, Settings.Instance.MaxNominations);
                if (!Settings.Instance.IgnoreCompanionLimit)
                {
                    nominations = Math.Min(nominations, Clan.PlayerClan.CompanionLimit - Clan.PlayerClan.Companions.Count);
                }
                MBInformationManager.ShowMultiSelectionInquiry(
                    new(
                        GameTexts.FindText("DistServ_inquiry_title", "distinguished").ToString(),
                        GameTexts.FindText("DistServ_inquiry_text", "distinguished").SetTextVariable("N", nominations).ToString(),
                        this.GenInquiryelements(coList, killCounts), true, 0, nominations, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("DistServ_inquiry_choice", "Random").ToString(), new Action<List<InquiryElement>>(OnNomineeSelect), null, ""), true);
                return;
            }
        }
        /// <summary>
        /// Take a character object list and killcount, creates a corresponding list of InquiryElements showing the unit's preview and killcount tooltip
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="kills"></param>
        public List<InquiryElement> GenInquiryelements(List<CharacterObject> characters, List<int> kills)
        {
            List<InquiryElement> ies = new();
            for (int q = 0; q < characters.Count; q++)
            {
                if (MobileParty.MainParty.MemberRoster.Contains(characters[q]))
                {
                    ies.Add(new((object)characters[q], characters[q].Name.ToString(), new(CharacterCode.CreateFrom((BasicCharacterObject)characters[q])), true, GameTexts.FindText("DistServ_tip", "killcount").SetTextVariable("COUNT", kills[q]).ToString()));
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
                    float num2 = (float)this.rng.NextDouble();
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


        /// <summary>
        /// Assigns the new hero with skill points and attributes, Add party.
        /// </summary>
        /// <param name="baseCharacter"></param>
        /// <param name="kills"></param>
        /// <param name="pickSkills"></param>
        public void PromoteUnit(CharacterObject baseCharacter, int kills = -1, bool pickSkills = true)
        {
            Hero specialHero;
            try
            {
                CharacterObject newcharacterObj = Game.Current.ObjectManager.GetObject<CharacterObject>(baseCharacter.StringId);
                if (newcharacterObj == null)
                {
                    return;
                }
                specialHero = this.InitializePromotedHero(newcharacterObj);
            }
            catch (NullReferenceException)
            {
                return;
            }
            this.SetHeroParams(specialHero, baseCharacter.Name, kills, null);
        }
        internal void SetHeroParams(Hero hero, TextObject prevName, int kills = -1, Hero? partyLeader = null)
        {
            int skipToAssign = Settings.Instance.AdditionalSkillPoints + 50 * (partyLeader ?? Hero.MainHero).GetSkillValue(DefaultSkills.Leadership) / Settings.Instance.LeadershipPointsPer50ExtraPoints;
            if (kills > 0)
            {
                if (hero.CharacterObject.IsMounted)
                {
                    skipToAssign += Settings.Instance.SkillPointsPerExcessKill * (kills - Settings.Instance.EligibleKillCountCavalry);
                }
                else if (hero.CharacterObject.IsRanged)
                {
                    skipToAssign += Settings.Instance.SkillPointsPerExcessKill * (kills - Settings.Instance.EligibleKillCountRanged);
                }
                else
                {
                    skipToAssign += Settings.Instance.SkillPointsPerExcessKill * (kills - Settings.Instance.EligibleKillCountInfantry);
                }
            }


            if (Settings.Instance.RandomizedSkill)
            {
                for (int i = 1; i <= Settings.Instance.NumSkillRounds; i++)
                {
                    AssignSkillsRandomly(hero, skipToAssign / i, Settings.Instance.NumSkillBonuses);
                }
            }
            else
            {
                for (int i = 1; i <= Settings.Instance.NumSkillRounds; i++)
                {
                    AssignSkills(hero, skipToAssign / i, Settings.Instance.NumSkillBonuses, i, Settings.Instance.NumSkillRounds, prevName);
                }
            }
            int totalAtt = hero.HeroDeveloper.UnspentAttributePoints;
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2, false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 2, false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, 2, false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 2, false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 2, false);
            hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, 2, false);
            totalAtt -= 12;
            if (totalAtt > 0)
            {

                int toAdd;
                if (hero.CharacterObject.IsMounted)
                {
                    toAdd = rng.Next(3);
                    hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, toAdd, false);
                    totalAtt -= toAdd;
                }
                else if (hero.CharacterObject.IsRanged)
                {
                    toAdd = rng.Next(3);
                    hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, toAdd, false);
                    totalAtt -= toAdd;
                }
                else
                {
                    toAdd = rng.Next(3);
                    hero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, toAdd, false);
                    totalAtt -= toAdd;
                }
                List<CharacterAttribute> shuffled_attrs = new(Attributes.All);
                Shuffle(shuffled_attrs);
                foreach (CharacterAttribute ca in shuffled_attrs)
                {
                    toAdd = rng.Next(2);
                    hero.HeroDeveloper.AddAttribute(ca, toAdd, false);
                    totalAtt -= toAdd;
                    if (totalAtt <= 0)
                        break;
                }
            }
            if (Settings.Instance.FillInPerks)
            {
                hero.DevelopCharacterStats();
            }
            hero.HeroDeveloper.UnspentAttributePoints = 0;
        }

        /// <summary>
        /// Sets new hero's skill point given player's skill seelection
        /// capped out at 300 to avoid problems
        /// </summary>
        /// <param name="specialHero"></param>
        /// <param name="skPointsAasign"></param>
        /// <param name="numSelectedSkills"></param>
        /// <param name="n"></param>
        /// <param name="total"></param>
        /// <param name="prev"></param>
        public void AssignSkills(Hero specialHero, int skPointsAasign, int numSelectedSkills, int n, int total, TextObject prev)
        {
            List<InquiryElement> iqes = new();
            List<SkillObject> soList = new() { DefaultSkills.Scouting, DefaultSkills.Crafting, DefaultSkills.Athletics, DefaultSkills.Riding, DefaultSkills.Tactics, DefaultSkills.Roguery, DefaultSkills.Charm, DefaultSkills.Leadership, DefaultSkills.Trade, DefaultSkills.Steward, DefaultSkills.Medicine, DefaultSkills.Engineering };
            foreach (SkillObject so in soList)
            {
                if (specialHero.GetSkillValue(so) < 300)
                    iqes.Add(new($"{so.StringId}_bonus", GameTexts.FindText("DistServ_bonus_title", so.StringId.ToString()).ToString(), null, true, GameTexts.FindText("DistServ_bonus_hint", "text").SetTextVariable("COUNT", skPointsAasign).SetTextVariable("SKILLNAME", so.Name.ToString()).ToString()));
            }
            MultiSelectionInquiryData msid = new(
                GameTexts.FindText("DistServ_inquiry_title", "select").SetTextVariable("COUNT", n).SetTextVariable("TOTAL", total).ToString(),
                GameTexts.FindText("DistServ_inquiry_text", "select").SetTextVariable("NAME", specialHero.Name).SetTextVariable("PREV", prev).SetTextVariable("COUNT", Settings.Instance.NumSkillBonuses).ToString(),
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
                        if (Settings.Instance.ShowCautionText)
                            InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "014levelerro").ToString(), Colors.Yellow));
                    }
                }
            }),
            null);
            MBInformationManager.ShowMultiSelectionInquiry(msid, true);
        }
        /// <summary>
        /// TODO: why separated from AssignSkillsRandomly function?
        /// </summary>
        /// <param name="specialHero"></param>
        /// <param name="skPointsAasign"></param>
        /// <param name="numSelectedSkills"></param>
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
                    specialHero.HeroDeveloper.ChangeSkillLevel(sk, diff);
                }
            }
            try
            {
                specialHero.CheckInitialLevel();
            }
            catch (Exception e)
            {
                if (Settings.Instance.ShowCautionText)
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
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// if you recruit a qualified unit, turn them into a hero immediately
        /// </summary>
        /// <param name="troop"></param>
        /// <param name="amount"></param>
        public void RecruitAsHero(CharacterObject troop, int amount)
        {
            if (!IsSoldierQualified(troop) || !MobileParty.MainParty.MemberRoster.Contains(troop))
                return;
            for (int i = 0; i < amount; i++)
            {
                if (!Settings.Instance.IgnoreCompanionLimit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
                {
                    MobileParty.MainParty.MemberRoster.RemoveTroop(troop, i);
                    return;
                }
                PromotionManager.__instance.PromoteUnit(troop);
            }
            MobileParty.MainParty.MemberRoster.RemoveTroop(troop, amount);
        }

        /// <summary>
        /// for upgraded units
        /// </summary>
        /// <param name="upgradeFromTroop"></param>
        /// <param name="upgradeToTroop"></param>
        /// <param name="number"></param>
        public void UpgradeToHero(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
        {
            if (!IsSoldierQualified(upgradeToTroop))
                return;
            for (int i = 0; i < number; i++)
            {
                if (!Settings.Instance.IgnoreCompanionLimit && Clan.PlayerClan.Companions.Count >= Clan.PlayerClan.CompanionLimit)
                {
                    MobileParty.MainParty.MemberRoster.RemoveTroop(upgradeToTroop, i);
                    return;
                }
                PromotionManager.__instance.PromoteUnit(upgradeToTroop);
            }
            MobileParty.MainParty.MemberRoster.RemoveTroop(upgradeToTroop, number);
        }

        /// <summary>
        /// Console commands to both test out functionality, and allow players to set up
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
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

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(GameTexts.FindText("DistServ_inquiry_title", "console").ToString(), GameTexts.FindText("DistServ_inquiry_text", "console").ToString(), PromotionManager.__instance.GenInquiryelements(cos, fauxKills), true, 0, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("DistServ_inquiry_choice", "Random").ToString(), new Action<List<InquiryElement>>(PromotionManager.__instance.OnNomineeSelect), null, ""), true);

            return "Dialog Generated";
        }


        /// <summary>
        /// manipulates promotion event for NPC parties. Scans the battle at each event and invoke the promotion event at random
        /// </summary>
        /// <param name="mapEvent"></param>
        public void MapEventEnded(MapEvent mapEvent)
        {
            //while this kinda feels like cheating, it's in C# so it's not like
            //performance is the goal anyway
            try
            {
                //only care about decisive field battles
                if (!(mapEvent.HasWinner))
                    return;
                //look at winning side
                foreach (MapEventParty p in mapEvent.PartiesOnSide(mapEvent.WinningSide))
                {
                    if (p == null || p.Party == PartyBase.MainParty || p.Party.LeaderHero?.Clan == Clan.PlayerClan)
                    {
                        continue;
                    }
                    if (p.Party.LeaderHero != null)
                    {
                        if (this.rng.NextDouble() > Settings.Instance.ChancePromotionAI)
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
                        if (companionsInParty < Settings.Instance.MaxPartyCompanionAI && qualified.Count > 0)
                        {
                            this.PromoteToParty(qualified[0], p.Party.MobileParty);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// PromoteUnit for NPC party
        /// TODO: why separete from the PromoteUnit
        /// </summary>
        /// <param name="baseCharacter"></param>
        /// <param name="party"></param>
        public void PromoteToParty(CharacterObject baseCharacter, MobileParty party)
        {
            if (baseCharacter == null || party == null)
            {
                return;
            }
            Hero? partyLeader = party?.LeaderHero;
            if (partyLeader == null)
            {
                return;
            }
            if (partyLeader.IsMinorFactionHero)
            {
                return;
            }
            Hero specialHero;
            try
            {
                specialHero = this.InitializePromotedHero(baseCharacter, party);
            }
            catch (NullReferenceException)
            {
                return;
            }
            if (specialHero.CharacterObject.IsMounted)
            {
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2 + rng.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 1 + rng.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 4 + rng.Next(3), false);
            }
            else if (specialHero.CharacterObject.IsRanged)
            {
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 2 + rng.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 4 + rng.Next(3), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 1 + rng.Next(2), false);
            }
            else
            {
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Vigor, 3 + rng.Next(3), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 2 + rng.Next(2), false);
                specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Endurance, 2 + rng.Next(2), false);
            }
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Cunning, 1 + rng.Next(3), false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Social, 1 + rng.Next(3), false);
            specialHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Intelligence, 1 + rng.Next(3), false);
            List<SkillObject> shuffledSkills = new(Skills.All);
            Shuffle(shuffledSkills);
            int skipToAssign = Settings.Instance.AdditionalSkillPoints + 50 * partyLeader.GetSkillValue(DefaultSkills.Leadership) / Settings.Instance.LeadershipPointsPer50ExtraPoints;
            foreach (SkillObject sk in shuffledSkills)
            {
                int bonus;
                if (sk == DefaultSkills.OneHanded || sk == DefaultSkills.TwoHanded || sk == DefaultSkills.Polearm || sk == DefaultSkills.Bow || sk == DefaultSkills.Crossbow || sk == DefaultSkills.Throwing)
                {
                    bonus = rng.Next(10) + rng.Next(15);
                }
                else
                {
                    bonus = rng.Next(10) + rng.Next(15) + rng.Next(25);
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
                specialHero.HeroDeveloper.UnspentFocusPoints += specialHero.Level;

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

        /// <summary>
        /// Initializes Hero object from a base characterObject
        /// Creates instance from a wanderer template, decide the name, add default equipments, and have it join the party
        /// </summary>
        /// <param name="baseCharacter"></param>
        /// <param name="party"></param> you should null if the target party is the player's party
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        internal Hero InitializePromotedHero(CharacterObject baseCharacter, MobileParty? party = null)
        {
            bool isPlayerCase = party == MobileParty.MainParty || party == null;
            MobileParty targetParty = isPlayerCase ? MobileParty.MainParty : party;
            Hero partyLeader = (isPlayerCase ? Hero.MainHero : party?.LeaderHero) ?? throw new NullReferenceException();
            CharacterObject wanderer = baseCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == baseCharacter.IsFemale && x.CivilianEquipments != null));
            if (wanderer == null)
            {
                if (Settings.Instance.ShowCautionText)
                    InformationManager.DisplayMessage(new(
                        GameTexts.FindText("DistServ_Caution", "009nowanderer").SetTextVariable("CULTURE", baseCharacter.Culture.Name).ToString(), Colors.Yellow));
                wanderer = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == baseCharacter.IsFemale && x.CivilianEquipments != null));
            }
            if (wanderer == null)
            {
                if (Settings.Instance.ShowCautionText)
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Warn", "010templete").ToString(), Colors.Red));
                wanderer = CharacterObject.PlayerCharacter.Culture.NotableAndWandererTemplates.GetRandomElementWithPredicate<CharacterObject>((Func<CharacterObject, bool>)(x => x.Occupation == Occupation.Wanderer && x.IsFemale == baseCharacter.IsFemale));
            }
            if (wanderer == null)
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Warn", "011wanderernotfound").ToString(), Colors.Red));
                throw new NullReferenceException();
            }
            Hero specialHero = HeroCreator.CreateSpecialHero(wanderer, null, null, null, 20 + Binomial.Sample(this.rng, 0.2, 30));
            specialHero.Culture = wanderer.Culture;
            specialHero.CharacterObject.IsBasicTroop = false;
            HeroHelper.SpawnHeroForTheFirstTime(
                specialHero,
                SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown && x.Culture == specialHero.Culture) ?? SettlementHelper.FindRandomSettlement());  // I don't make sure but an errorful characterobject appears on the encyclopedia causes the game crash without this function.
            specialHero.ChangeState(Hero.CharacterStates.Active);
            AddCompanionAction.Apply(partyLeader.Clan, specialHero); // hero.Clan shouldn't be null.
            AddHeroToPartyAction.Apply(specialHero, targetParty, true);
            // (isPlayerCase ? MobileParty.MainParty : party).AddElementToMemberRoster(specialHero.CharacterObject, 1);
            if (NameList.IsFileExists() && isPlayerCase)
            {
                TextObject newName = NameList.PullOutNameFromExternalFile();
                if (newName.ToString() != "")
                {
                    specialHero.SetName(NameList.DrawNameFormat(wanderer.IsRanged, wanderer.FirstBattleEquipment, wanderer.Culture).SetTextVariable("FIRSTNAME", newName), newName);
                }
            }
            else
            {
                specialHero.SetName(NameList.DrawNameFormat(wanderer.IsRanged, wanderer.FirstBattleEquipment, wanderer.Culture).SetTextVariable("FIRSTNAME", specialHero.FirstName), specialHero.FirstName);
            }
            if (isPlayerCase)
            {
                specialHero.SetHasMet();
            }
            else if (Settings.Instance.NotifyNPCPromotion)
            {
                InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_notify", "npcpromotion").SetTextVariable("NAME", specialHero.Name).ToString(), Colors.White, ""));
            }
            AddTraitVariance(specialHero);
            specialHero.HeroDeveloper.SetInitialLevel(wanderer.Level);
            // Default formation class seems to be read only, so I could't change it // TODO
            specialHero.CharacterObject.DefaultFormationGroup = wanderer.DefaultFormationGroup;
            CampaignEventDispatcher.Instance.OnHeroCreated(specialHero, false);
            float adjustedCost = Settings.Instance.PromotionCost;
            if (partyLeader.GetPerkValue(DefaultPerks.Trade.GreatInvestor))
            {
                adjustedCost *= 0.7f;
            }
            if (partyLeader.GetPerkValue(DefaultPerks.Steward.PaidInPromise))
            {
                adjustedCost *= 0.75f;
            }
            GiveGoldAction.ApplyBetweenCharacters(partyLeader, specialHero, (int)adjustedCost);
            try
            {
                if (isPlayerCase)  // TODO: this conditioning really needed?
                {
                    if (MyLittleWarbandLoaded)
                    {
                        InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Info", "compatible").ToString(), Colors.Yellow));
                        specialHero.BattleEquipment.FillFrom(baseCharacter.FirstBattleEquipment);
                        specialHero.CivilianEquipment.FillFrom(baseCharacter.FirstCivilianEquipment);
                    }
                    else
                    {
                        specialHero.BattleEquipment.FillFrom(baseCharacter.RandomBattleEquipment);
                        specialHero.CivilianEquipment.FillFrom(baseCharacter.RandomCivilianEquipment);
                    }
                }
                else
                {
                    specialHero.BattleEquipment.FillFrom(baseCharacter.FirstBattleEquipment);// character.RandomBattleEquipment);
                    specialHero.CivilianEquipment.FillFrom(baseCharacter.FirstCivilianEquipment);// character.RandomCivilianEquipment);
                }
                this.AdjustEquipment(specialHero);
            }
            catch (Exception e)
            {
                if (Settings.Instance.ShowCautionText)
                {
                    InformationManager.DisplayMessage(new(GameTexts.FindText("DistServ_Caution", "013equipment").ToString(), Colors.Yellow));
                    Debug.Print(GameTexts.FindText("DistServ_Warn", "internalerr").SetTextVariable("TEXT", e.Message).ToString());
                }
            }
            // copy original skillpoint to new object with capping out.
            // TODO: why so intricate?
            if (isPlayerCase)
            {
                Dictionary<SkillObject, int> baselineSkills = new();
                foreach (SkillObject sk in Skills.All)
                {
                    baselineSkills[sk] = Math.Min(baseCharacter.GetSkillValue(sk), 300);
                    // specialHero.HeroDeveloper.SetInitialSkillLevel(sk, baseCharacter.GetSkillValue(sk));
                }
                foreach (SkillObject sk in Skills.All)
                {
                    int currentSkill = specialHero.GetSkillValue(sk);
                    specialHero.HeroDeveloper.ChangeSkillLevel(sk, baselineSkills[sk] - currentSkill);
                }
            }
            else
            {
                foreach (SkillObject sk in Skills.All)
                {
                    specialHero.HeroDeveloper.ChangeSkillLevel(sk, baseCharacter.GetSkillValue(sk), false);
                }
            }
            return specialHero;
        }

        /// <summary>
        /// Add the dialog options to the game; rename the companion's name, transfer to another party, 
        /// take back a companion from another party, poach a companion from a defeated party
        /// </summary>
        /// <param name="campaignGameStarter"></param>
        public static void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddPlayerLine("companion_change_name_start", "hero_main_options", "companion_change_name_confirm", GameTexts.FindText("DistServ_dialog", "rename").ToString(), new(GetNamechangecondition), new(GetNamechanceconsequence), 100, null, null);
            campaignGameStarter.AddDialogLine("companion_change_name_confirm", "companion_change_name_confirm", "hero_main_options", GameTexts.FindText("DistServ_dialog", "answer").ToString(), null, null, 100, null);
            campaignGameStarter.AddPlayerLine("companion_transfer_start", "hero_main_options", "companion_transfer_confirm", GameTexts.FindText("DistServ_dialog", "transfer").ToString(), new(GetGiveCompToClanPartyCondition), new(GetGiveCompToClanPartyConsequence), 100, null, null);
            campaignGameStarter.AddDialogLine("companion_transfer_confirm", "companion_transfer_confirm", "hero_main_options", GameTexts.FindText("DistServ_dialog", "answer").ToString(), null, null, 100, null);
            campaignGameStarter.AddPlayerLine("companion_takeback_start", "hero_main_options", "companion_takeback_confirm", GameTexts.FindText("DistServ_dialog", "takeback").ToString(), new(GetTakeCompFromClanPartyCondition), new(GetTakeCompFromClanPartyConsequence), 100, null, null);
            campaignGameStarter.AddDialogLine("companion_takeback_confirm", "companion_takeback_confirm", "hero_main_options", GameTexts.FindText("DistServ_dialog", "answer").ToString(), null, null, 100, null);
            campaignGameStarter.AddPlayerLine("enemy_comp_recruit_1", "defeated_lord_answer", "companion_poach_confirm", GameTexts.FindText("DistServ_dialog", "poach").ToString(), new(PromotionManager.GetCapturedAIWandererCondition), null, 100, null, null); //new ConversationSentence.OnClickableConditionDelegate(CanConvertWanderer)
            campaignGameStarter.AddDialogLine("enemy_comp_recruit_2", "companion_poach_confirm", "close_window", "{RECRUIT_RESPONSE}", null, null, 100, null);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static bool GetNamechangecondition()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.IsPlayerCompanion;
        }
        private static void GetNamechanceconsequence()
        {
            InformationManager.ShowTextInquiry(new(GameTexts.FindText("DistServ_inquiry_title", "newname").ToString(), string.Empty, true, true, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), new Action<string>(PromotionManager.ChangeHeroName), null, false), false);

        }
        private static void ChangeHeroName(string s)
        {
            if (s != "")
            {
                Hero.OneToOneConversationHero.SetName(new TextObject(s), new TextObject(s));
            }
        }

        /// <summary>
        /// Companion transferrence logic.
        /// </summary>
        /// <returns></returns>
        private static bool GetGiveCompToClanPartyCondition()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && (Hero.OneToOneConversationHero.IsPartyLeader);
        }
        private static void GetGiveCompToClanPartyConsequence()
        {
            MBInformationManager.ShowMultiSelectionInquiry(new(GameTexts.FindText("DistServ_inquiry_title", "trsfhero").ToString(), GameTexts.FindText("DistServ_inquiry_text", "trsfhero").SetTextVariable("HERO", Hero.OneToOneConversationHero.Name).ToString(), PromotionManager.GenTransferList(PromotionManager.GetPlayerPartyHeroCOs()), true, 0, PartyBase.MainParty.MemberRoster.Count, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("DistServ_inquiry_choice", "Nobody").ToString(), new Action<List<InquiryElement>>(PromotionManager.TransferCompsToConversationParty), null, ""), true);
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
        /// <summary>
        /// returns a list of heros in the player's party
        /// </summary>
        /// <returns></returns>
        private static List<CharacterObject> GetPlayerPartyHeroCOs()
        {
            List<CharacterObject> hs = new();
            foreach (TroopRosterElement tre in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                CharacterObject co = tre.Character;
                if (!co.IsHero || co.IsPlayerCharacter)
                    continue;
                hs.Add(co);
            }
            return hs;
        }

        /// <summary>
        /// Take heros from your clan's party logic
        /// </summary>
        /// <returns></returns>
        private static bool GetTakeCompFromClanPartyCondition()
        {
            return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && (Hero.OneToOneConversationHero.IsPartyLeader);
        }

        /// <summary>
        /// 
        /// </summary>
        private static void GetTakeCompFromClanPartyConsequence()
        {
            MBInformationManager.ShowMultiSelectionInquiry(new(GameTexts.FindText("DistServ_inquiry_title", "trsfhero").ToString(), GameTexts.FindText("DistServ_inquiry_text", "trsfhero").SetTextVariable("HERO", Hero.OneToOneConversationHero.Name).ToString(), PromotionManager.GenTransferList(PromotionManager.GetConversationPartyHeros(), false), true, 0, PartyBase.MainParty.MemberRoster.Count, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("DistServ_inquiry_chice", "Nobody").ToString(), new Action<List<InquiryElement>>(PromotionManager.TransferCompsFromConversationParty), null, ""), true);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inquiryList"></param>
        private static void TransferCompsFromConversationParty(List<InquiryElement> inquiryList)
        {
            foreach (InquiryElement ie in inquiryList)
            {
                CharacterObject co = (CharacterObject)(ie.Identifier);
                Hero h = co.HeroObject;
                AddHeroToPartyAction.Apply(h, MobileParty.MainParty, true);
            }
        }
        /// <summary>
        /// generates a list of InquiryElements from a list of CharacterObjects
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="isMainPartyRequired"></param>
        /// <returns></returns>
        public static List<InquiryElement> GenTransferList(List<CharacterObject> characters, bool isMainPartyRequired = true)
        {
            List<InquiryElement> ies = new();

            foreach (CharacterObject co in characters)
            {
                if ((!isMainPartyRequired) || (isMainPartyRequired && MobileParty.MainParty.MemberRoster.Contains(co)))
                {
                    ies.Add(new((object)co, co.Name.ToString(), new(CharacterCode.CreateFrom((BasicCharacterObject)co)), true, " kills"));
                }
            }
            return ies;
        }
        /// <summary>
        /// Gets list of heros in the party of the hero you are conversing with
        /// </summary>
        /// <returns></returns>
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

        //your "values" align more closely to theirs
        /// <summary>
        /// Condition for whether a captured enemy wanderer will consider switching sides if your "values" align more closely to theirs
        /// </summary>
        /// <returns></returns>
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
