/*
 * Author: Thor Tronrud
 * SubModule.cs:
 * 
 * Bannerlord's required SubModule class. Provides access to
 * the virtual methods we want to implement to load the mod into
 * the game.
 * 
 * This mod does not change save files - and only activates on game start/load
 * so it can be turned on and off as desired.
 * In the starting methods I also add several event listeners based on values from
 * the settings file, so if they're deactivated they aren't even invoked.
 */

using DistinguishedServiceRedux.settings;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace DistinguishedServiceRedux
{
    public class SubModule : MBSubModuleBase
    {
        public static readonly string moduleName = "DistinguishedServiceRedux";
        public static SubModule? instance;
        public bool gamestarted = false;
        private static PromotionManager? _pm = null;

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(Game.Current.GameType is Campaign))
                return; // OnCampaignStart doesn't always mean it's a campaign game

            try
            {
                if (PromotionManager.__instance == null)
                {
                    _pm = new PromotionManager();
                }
                else
                {
                    _pm = PromotionManager.__instance;
                }

                ((CampaignGameStarter)gameStarterObject).AddBehavior(new DSBattleBehavior());

                //ai gaining companions
                CampaignEvents.MapEventEnded.AddNonSerializedListener((object)this, new Action<MapEvent>(_pm.MapEventEnded));

                //for alternative nomination behaviour
                if (Settings.Instance.UpgradeToHero)
                {
                    CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, new Action<CharacterObject, CharacterObject, int>(_pm.UpgradeToHero));
                    CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, new Action<CharacterObject, int>(_pm.RecruitAsHero));

                }

                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("DistServ_Info", "loaded").ToString(), Colors.White));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("DistServ_Error", "problem").SetTextVariable("ERROR", ex.ToString()).ToString(), Colors.White));
            }
            gamestarted = true;
        }

        public override void OnGameLoaded(Game game, object initializerObject)
        {
            if (game.GameType is not Campaign)
                return;
            bool reload = false;
            try
            {
                if (PromotionManager.__instance == null)
                {
                    _pm = new PromotionManager();
                }
                else
                {
                    _pm = PromotionManager.__instance;
                    reload = true;
                }

                if (!gamestarted)
                    ((CampaignGameStarter)initializerObject).AddBehavior(new DSBattleBehavior());

                //ai gaining companions
                CampaignEvents.MapEventEnded.AddNonSerializedListener((object)this, new Action<MapEvent>(_pm.MapEventEnded));

                if (Settings.Instance.UpgradeToHero)
                {
                    CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, new Action<CharacterObject, CharacterObject, int>(_pm.UpgradeToHero));
                    CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, new Action<CharacterObject, int>(_pm.RecruitAsHero));
                }

                if (!reload)
                    InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("DistServ_Info", "loaded").ToString(), Colors.White));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("DistServ_Error", "problem").SetTextVariable("ERROR", ex.ToString()).ToString(), Colors.White));
            }
            gamestarted = false;
        }

        //Once the game's been fully loaded, we check whether MLWB is loaded
        //and set the state accordingly
        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            PromotionManager.MyLittleWarbandLoaded = CheckMLWBLoaded();
            gamestarted = false;
        }
        //stupid function to determine if my little warband is loaded
        //because the default, random equipment selection will catch all the empty
        //equipment slots because the MLWB team decided to fuck with things.
        //It's ok, I'm not salty that *I'm* the one making a fix.
        public bool CheckMLWBLoaded()
        {
            foreach (Assembly assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assem.FullName.Contains("MyLittleWarband"))
                    return true;
            }
            return false;
        }
    }
}
