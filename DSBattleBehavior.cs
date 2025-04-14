/*
 * Author: Thor Tronrud
 */

using DistinguishedServiceRedux.settings;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DistinguishedServiceRedux
{
    internal class DSBattleLogic : MissionLogic
    {
        public DSBattleLogic()
        {
            if (PromotionManager.__instance == null)
                return;
            PromotionManager.__instance.nominations = new();
            PromotionManager.__instance.killcounts = new();
        }
        private int SumKillCountByNonHero()
        {
            int totalKillCount = 0;
            foreach (Agent ag in Mission.Current.PlayerTeam.ActiveAgents)
            {
                if (!ag.IsHero)
                {
                    totalKillCount += ag.KillCount;
                }
            }
            return totalKillCount;
        }
        private List<float> GetKillCounts()
        {
            List<float> kills = new();
            foreach (Agent ag in Mission.Current.PlayerTeam.ActiveAgents)
            {
                if (!ag.IsHero)
                {
                    kills.Add((float)ag.KillCount);
                }
            }
            return kills;
        }
        private double GetPercentile(IEnumerable<float> seq, double percentile)
        {
            var elements = seq.ToArray();
            Array.Sort(elements);
            double realIndex = percentile * (elements.Length - 1);
            int index = (int)realIndex;
            double frac = realIndex - index;
            if (index + 1 < elements.Length)
                return elements[index] * (1 - frac) + elements[index + 1] * frac;
            else
                return elements[index];
        }
        /// <summary>
        /// Overrides showbattleresults because it fires before any loading screen has had the chance to pop up
        /// That has previously caused a horrible bug where options would appear *behind* the loading screen, causing infinite hang
        /// </summary>
        ///
        public override void ShowBattleResults()
        {
            if (PromotionManager.__instance == null)
                return;
            PromotionManager.__instance.nominations.Clear();
            PromotionManager.__instance.killcounts.Clear();
            if (Mission.Current.Mode == MissionMode.Conversation || Mission.Current.Mode == MissionMode.StartUp)
                return;
            if (Mission.Current.CombatType == Mission.MissionCombatType.ArenaCombat)// || !Mission.Current.IsFieldBattle)
                return;
            if (SumKillCountByNonHero() <= 0)
                return;
            float qKills = (float)GetPercentile(GetKillCounts(), Settings.Instance.EligiblePercentile);
            foreach (Agent ag in Mission.Current.PlayerTeam.ActiveAgents)
            {
                if (ag.IsHero || ag.Origin == null || (PartyBase)ag.Origin.BattleCombatant == null || ((PartyBase)ag.Origin.BattleCombatant).MobileParty == null || !((PartyBase)ag.Origin.BattleCombatant).MobileParty.IsMainParty)
                    continue;
                CharacterObject co = CharacterObject.Find(ag.Character.StringId);
                if (!PartyBase.MainParty.MemberRoster.Contains(co) || !MobileParty.MainParty.MemberRoster.Contains(co))
                    continue;
                // FIXME: This can hit false positives in certain situations, but those edge cases are harder to fix and more rare, so we'll just settle for something that works for now...
                int cutoffKills;
                if (co.IsRanged)
                {
                    cutoffKills = Settings.Instance.EligibleKillCountRanged;
                }
                else if (co.IsMounted)
                {
                    cutoffKills = Settings.Instance.EligibleKillCountCavalry;
                }
                else
                {
                    cutoffKills = Settings.Instance.EligibleKillCountInfantry;
                }
                bool qualified = (Settings.Instance.EligiblePercentile <= 0 || ag.KillCount > MathF.Ceiling(qKills));
                if (qualified && ag.KillCount >= cutoffKills)
                {
                    if (PromotionManager.IsSoldierQualified(co))
                    {
                        PromotionManager.__instance.nominations.Add(co);
                        PromotionManager.__instance.killcounts.Add(ag.KillCount);
                    }
                }
            }
            PromotionManager.__instance.OnPCBattleEndedResults();
        }
    }
    /// <summary>
    /// Class to add behaviour to ongoing battle
    /// </summary>
    internal class DSBattleBehavior : CampaignBehaviorBase
    {
        public DSBattleBehavior()
        {
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener((object)this, new Action<IMission>(this.FindBattle));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(PromotionManager.AddDialogs));
        }
        public override void SyncData(IDataStore dataStore)
        {
        }
        public void FindBattle(IMission misson)
        {
            if (((Mission)misson).CombatType > Mission.MissionCombatType.Combat || !((NativeObject)Mission.Current.Scene != null))
                return;
            if (Mission.Current.HasMissionBehavior<DSBattleLogic>())
            {
                return;
            }
            Mission.Current.AddMissionBehavior(new DSBattleLogic());
        }
    }
}
