using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BannerlordTwitch;
using BannerlordTwitch.Helpers;
using BannerlordTwitch.Localization;
using BannerlordTwitch.Rewards;
using BannerlordTwitch.UI;
using BannerlordTwitch.Util;
using JetBrains.Annotations;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using YamlDotNet.Serialization;

namespace BLTAdoptAHero
{
    /// <summary>
    /// Requested by Maku ("The Beast"): events that fire depending on the map and where the player
    /// currently is.
    ///
    /// Deliberately data-driven rather than a fixed list of hard-coded events: the streamer writes
    /// their own in BLT Configure, because "an event for my map" is by definition specific to the
    /// campaign being played. Conditions are matched against the player's current surroundings,
    /// and every condition left blank simply does not restrict anything.
    /// </summary>
    [LocDisplayName("{=mapev001}Map Event")]
    public class MapEventDef : ICloneable, INotifyPropertyChanged
    {
        [LocDisplayName("{=mapev002}Name"),
         LocDescription("{=mapev003}Name of this event, shown only in the config"),
         PropertyOrder(1), UsedImplicitly]
        public string Name { get; set; } = "New Event";

        [LocDisplayName("{=mapev004}Enabled"),
         PropertyOrder(2), UsedImplicitly]
        public bool Enabled { get; set; } = true;

        [LocDisplayName("{=mapev005}Chance Per Day Percent"),
         LocDescription("{=mapev006}Chance per in-game day that this event fires, while its conditions are met"),
         PropertyOrder(3), Range(0f, 100f), UsedImplicitly]
        public float ChancePerDayPercent { get; set; } = 10f;

        #region Conditions
        [LocDisplayName("{=mapev007}Terrain"),
         LocDescription("{=mapev008}Comma-separated terrain types the player must be on, e.g. 'Forest,Mountain'. Valid names include Plain, Forest, Mountain, Steppe, Desert, Snow, Swamp, Water, Bridge, River, Lake, Fording, Canyon, RuralArea. Blank means any terrain."),
         LocCategory("Conditions", "{=mapev009}Conditions"),
         PropertyOrder(4), UsedImplicitly]
        public string Terrain { get; set; } = "";

        [LocDisplayName("{=mapev010}Near Settlement"),
         LocDescription("{=mapev011}Comma-separated settlement names; the event only fires when the player is within Near Settlement Distance of one of them. Blank means anywhere."),
         LocCategory("Conditions", "{=mapev009}Conditions"),
         PropertyOrder(5), UsedImplicitly]
        public string NearSettlement { get; set; } = "";

        [LocDisplayName("{=mapev012}Near Settlement Distance"),
         LocDescription("{=mapev013}How close the player must be to one of the named settlements, in map units"),
         LocCategory("Conditions", "{=mapev009}Conditions"),
         PropertyOrder(6), UsedImplicitly]
        public float NearSettlementDistance { get; set; } = 15f;

        [LocDisplayName("{=mapev014}Culture"),
         LocDescription("{=mapev015}Comma-separated cultures; the event only fires when the nearest settlement belongs to one of them, by name or id. Blank means any culture."),
         LocCategory("Conditions", "{=mapev009}Conditions"),
         PropertyOrder(7), UsedImplicitly]
        public string Culture { get; set; } = "";

        [LocDisplayName("{=mapev016}Requires Night"),
         LocDescription("{=mapev017}Only fire at night"),
         LocCategory("Conditions", "{=mapev009}Conditions"),
         PropertyOrder(8), UsedImplicitly]
        public bool RequiresNight { get; set; } = false;
        #endregion

        #region Effects
        [LocDisplayName("{=mapev018}Message"),
         LocDescription("{=mapev019}Announced in chat and the game feed when the event fires. {HERO} is replaced with the name of the hero who gets the reward, if any."),
         LocCategory("Effects", "{=mapev020}Effects"),
         PropertyOrder(9), UsedImplicitly]
        public string Message { get; set; } = "Something stirs nearby...";

        [LocDisplayName("{=mapev021}Gold Reward"),
         LocDescription("{=mapev022}Gold given to one random adopted hero. 0 for none."),
         LocCategory("Effects", "{=mapev020}Effects"),
         PropertyOrder(10), UsedImplicitly]
        public int GoldReward { get; set; } = 0;

        [LocDisplayName("{=mapev023}XP Reward"),
         LocDescription("{=mapev024}XP given to one random adopted hero. 0 for none."),
         LocCategory("Effects", "{=mapev020}Effects"),
         PropertyOrder(11), UsedImplicitly]
        public int XPReward { get; set; } = 0;

        [LocDisplayName("{=mapev025}Reward All Heroes"),
         LocDescription("{=mapev026}Give the reward to every adopted hero instead of one random one"),
         LocCategory("Effects", "{=mapev020}Effects"),
         PropertyOrder(12), UsedImplicitly]
        public bool RewardAllHeroes { get; set; } = false;
        #endregion

        public override string ToString() => $"{Name}{(Enabled ? "" : " (disabled)")}";

        public object Clone() => CloneHelpers.CloneProperties(this);

        public event PropertyChangedEventHandler PropertyChanged;
    }

    [LocDisplayName("{=mapev027}Map Events")]
    internal class GlobalMapEventConfig : IDocumentable
    {
        private const string ID = "Adopt A Hero - Map Events";

        internal static void Register() => ActionManager.RegisterGlobalConfigType(ID, typeof(GlobalMapEventConfig));
        internal static GlobalMapEventConfig Get() => ActionManager.GetGlobalConfig<GlobalMapEventConfig>(ID);

        [LocDisplayName("{=mapev028}Enabled"),
         LocDescription("{=mapev029}Master switch for map events"),
         PropertyOrder(1), UsedImplicitly]
        public bool Enabled { get; set; } = false;

        [LocDisplayName("{=mapev030}Max Events Per Day"),
         LocDescription("{=mapev031}Safety cap: at most this many map events fire in a single day, however many are configured"),
         PropertyOrder(2), Range(1, 20), UsedImplicitly]
        public int MaxEventsPerDay { get; set; } = 2;

        [LocDisplayName("{=mapev032}Events"),
         LocDescription("{=mapev033}The events themselves. Conditions left blank do not restrict anything, so an event with no conditions can fire anywhere."),
         Editor(typeof(DefaultCollectionEditor), typeof(DefaultCollectionEditor)),
         PropertyOrder(3), UsedImplicitly]
        public ObservableCollection<MapEventDef> Events { get; set; } = new();

        [YamlIgnore, Browsable(false)]
        public IEnumerable<MapEventDef> ValidEvents => Events?.Where(e => e is { Enabled: true }) ?? Enumerable.Empty<MapEventDef>();

        public void GenerateDocumentation(IDocumentationGenerator generator)
        {
            generator.PropertyValuePair("Enabled", Enabled.ToString());
            generator.PropertyValuePair("Max Events Per Day", MaxEventsPerDay.ToString());
            foreach (var e in Events ?? new())
            {
                generator.PropertyValuePair(e.Name, e.Message);
            }
        }
    }
}
