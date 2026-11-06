using System;
using System.Collections.Generic;

namespace SHI.Core
{
    public enum Res { Scrap, Battery, Food, Medicine }
    public enum Role { Rest, Store, Scrap, Food, Battery, Guard }
    public enum Health { Healthy, Injured, Dead, Gone }
    public enum Ration { Reduced, Normal, Generous }
    public enum Phase { Day, Night, End }
    public enum BuildingType { Shield, Warehouse, Quarters, Clinic, Workshop, Wall, Farm }
    public enum NightOrder { HoldWalls, Ambush, Overload }
    public enum IndoorActivity { Craft, Train, Care, Overtime }
    public enum EndReason { None, Survived, MoraleLost, Revolt, NoPeople }

    public sealed class Person
    {
        public string Id;
        public string Name;
        public string Title;
        public string Note;
        public Role Role;
        public Health Health = Health.Healthy;
        public bool Frail;
        public int Skill;
        public int InjuredDays;
        public readonly HashSet<string> Memory = new HashSet<string>();

        public bool Present => Health == Health.Healthy || Health == Health.Injured;
        public bool CanWork => Health == Health.Healthy && !Frail;
    }

    public sealed class Building
    {
        public BuildingType Type;
        public int X, Y, W, D;
        public bool Exposed;
    }

    public sealed class BuildingInfo
    {
        public string Name;
        public string Description;
        public int W, D;
        public int ScrapCost, BatteryCost;
    }

    public sealed class ChronicleEntry
    {
        public int Day;
        public string Text;
        public string Tag;
    }

    public sealed class EventOption
    {
        public string Text;
        public string Hint;
        public string Chronicle;
        public string Tag;
        public Action<Game> Apply;
        public Func<Game, bool> Available;
    }

    public sealed class GameEvent
    {
        public string Id;
        public string Title;
        public string SpeakerId;
        public bool Major;
        public string CauseTag;
        public string[] Foreshadows = new string[0];
        public int Priority;
        public Func<Game, bool> Condition;
        public Func<Game, string> Body;
        public EventOption[] Options;
    }

    public sealed class RaidResult
    {
        public bool Won;
        public int Attack;
        public int Defense;
        public string Summary;
    }
}
