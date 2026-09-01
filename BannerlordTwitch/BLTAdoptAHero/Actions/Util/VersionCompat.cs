using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace BLTAdoptAHero.Actions.Util
{
    // Game-1.3.15 native API shim. A handful of TaleWorlds APIs shifted shape between 1.3.15 and
    // 1.4.x; this build targets 1.3.15 only, so the call sites here go straight to the 1.3.15 shape.
    public static class VersionCompat
    {
        public static bool HasTradeAgreementCompat(this TradeAgreementsCampaignBehavior tradeBehavior, Kingdom a, Kingdom b)
        {
            return tradeBehavior.HasTradeAgreement(a, b);
        }

        public static int WarPartyLimitCompat(this Clan clan)
        {
            return clan.CommanderLimit;
        }

        public static IEnumerable<MobileParty> GetPartiesToCallToArmyCompat(this Campaign campaign, MobileParty leaderParty)
        {
            return campaign.Models.ArmyManagementCalculationModel.GetMobilePartiesToCallToArmy(leaderParty);
        }
    }
}
