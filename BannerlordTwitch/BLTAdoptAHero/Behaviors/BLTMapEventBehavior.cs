using System;
using System.Linq;
using BannerlordTwitch.Helpers;
using Helpers;
using BannerlordTwitch.Util;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BLTAdoptAHero
{
    /// <summary>
    /// Fires the events configured in <see cref="GlobalMapEventConfig"/> based on where the player
    /// currently is on the campaign map.
    ///
    /// Checked hourly rather than daily so the player's location actually matters - a daily check
    /// would test a position they left hours ago. The configured chance is stated per day, so the
    /// hourly roll is the daily chance divided across the day.
    /// </summary>
    public class BLTMapEventBehavior : CampaignBehaviorBase
    {
        public static BLTMapEventBehavior Current => Campaign.Current?.GetCampaignBehavior<BLTMapEventBehavior>();

        private int firedToday;
        private int lastDay = -1;

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnHourlyTick()
        {
            try
            {
                var cfg = GlobalMapEventConfig.Get();
                if (cfg?.Enabled != true) return;
                if (Mission.Current != null) return;

                var party = MobileParty.MainParty;
                if (party == null || Hero.MainHero == null) return;

                int today = (int)CampaignTime.Now.ToDays;
                if (today != lastDay)
                {
                    lastDay = today;
                    firedToday = 0;
                }
                if (firedToday >= cfg.MaxEventsPerDay) return;

                var terrain = GetCurrentTerrain(party);
                var nearest = SettlementHelper.FindNearestSettlementToMobileParty(party, MobileParty.NavigationType.Default);
                float nearestDistance = nearest == null
                    ? float.MaxValue
                    : party.GetPosition2D.Distance(nearest.GetPosition2D);

                foreach (var ev in cfg.ValidEvents.OrderBy(_ => MBRandom.RandomFloat))
                {
                    if (firedToday >= cfg.MaxEventsPerDay) return;
                    if (!Matches(ev, terrain, nearest, nearestDistance)) continue;

                    // The configured chance is per day; this runs 24 times a day.
                    if (MBRandom.RandomFloat * 100f >= ev.ChancePerDayPercent / 24f) continue;

                    Fire(ev);
                    firedToday++;
                }
            }
            catch (Exception ex)
            {
                Log.Exception($"{nameof(BLTMapEventBehavior)}", ex);
            }
        }

        private static TerrainType GetCurrentTerrain(MobileParty party)
        {
            var map = Campaign.Current?.MapSceneWrapper;
            if (map == null) return TerrainType.Plain;

            foreach (bool isOnLand in new[] { true, false })
            {
                var vec = new CampaignVec2(party.GetPosition2D, isOnLand);
                var face = map.GetFaceIndex(in vec);
                if (face.IsValid()) return map.GetFaceTerrainType(face);
            }
            return TerrainType.Plain;
        }

        private static bool Matches(MapEventDef ev, TerrainType terrain, Settlement nearest, float nearestDistance)
        {
            // Every condition left blank restricts nothing - an event with no conditions can fire
            // anywhere, which is the least surprising reading of an empty field.
            if (!ListContains(ev.Terrain, terrain.ToString())) return false;

            if (!string.IsNullOrWhiteSpace(ev.NearSettlement))
            {
                if (nearest == null) return false;
                if (nearestDistance > ev.NearSettlementDistance) return false;
                if (!ListContains(ev.NearSettlement, nearest.Name?.ToString())) return false;
            }

            if (!string.IsNullOrWhiteSpace(ev.Culture))
            {
                var culture = nearest?.Culture;
                if (culture == null) return false;
                if (!ListContains(ev.Culture, culture.StringId)
                    && !ListContains(ev.Culture, culture.Name?.ToString())) return false;
            }

            if (ev.RequiresNight && !Campaign.Current.IsNight) return false;

            return true;
        }

        /// <summary>
        /// Comma-separated match, case-insensitive and space-tolerant, matching how Blocked
        /// Cultures already works elsewhere in the config. An empty list matches everything.
        /// </summary>
        private static bool ListContains(string list, string value)
        {
            if (string.IsNullOrWhiteSpace(list)) return true;
            if (string.IsNullOrWhiteSpace(value)) return false;

            return list.Split(',')
                .Select(e => e.Trim())
                .Where(e => e.Length > 0)
                .Any(e => string.Equals(e, value.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static void Fire(MapEventDef ev)
        {
            var heroes = BLTAdoptAHeroCampaignBehavior.GetAllAdoptedHeroes()?.ToList();

            string heroName = "";
            if (ev.GoldReward != 0 || ev.XPReward != 0)
            {
                if (heroes == null || heroes.Count == 0)
                {
                    // Nobody to reward - still announce, the event happened either way.
                }
                else if (ev.RewardAllHeroes)
                {
                    foreach (var h in heroes) Reward(h, ev);
                    heroName = "everyone";
                }
                else
                {
                    var lucky = heroes.SelectRandom();
                    Reward(lucky, ev);
                    heroName = lucky.Name?.ToString() ?? "";
                }
            }

            string message = (ev.Message ?? "").Replace("{HERO}", heroName);
            if (!string.IsNullOrWhiteSpace(message))
            {
                Log.LogFeedEvent(message);
            }
        }

        private static void Reward(Hero hero, MapEventDef ev)
        {
            if (hero == null) return;
            if (ev.GoldReward != 0)
            {
                BLTAdoptAHeroCampaignBehavior.Current.ChangeHeroGold(hero, ev.GoldReward);
            }
            if (ev.XPReward != 0)
            {
                SkillXP.ImproveSkill(hero, ev.XPReward, SkillsEnum.All, auto: true);
            }
        }
    }
}
