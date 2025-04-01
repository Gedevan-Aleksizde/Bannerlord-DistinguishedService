using TaleWorlds.CampaignSystem.CharacterDevelopment;
namespace DistinguishedService.ext
{
    public static class IHeroDeveloperExt
    {
        public static void CheckInitialLevel(this IHeroDeveloper iHeroDeveloper)
        {
            if (iHeroDeveloper.Hero.Level < 1)
            {
                throw new exceptions.ValueException("Hero level is less than 1");

            }
            if (iHeroDeveloper.UnspentFocusPoints < 0)
            {
                throw new exceptions.ValueException("less than 0 free focus points");
            }
            if (iHeroDeveloper.UnspentAttributePoints < 0)
            {
                throw new exceptions.ValueException("less than 0 free attribute points");
            }
        }

    }
}
