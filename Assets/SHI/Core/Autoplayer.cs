using System;
using System.Collections.Generic;
using System.Linq;

namespace SHI.Core
{
    public sealed class AutoplayReport
    {
        public int Runs;
        public readonly Dictionary<string, int> EventCounts = new Dictionary<string, int>();
        public readonly Dictionary<EndReason, int> Endings = new Dictionary<EndReason, int>();
        public readonly List<string> Violations = new List<string>();
        public int TotalDays;
        public int Raids;
    }

    public static class Autoplayer
    {
        public static AutoplayReport Run(int runs)
        {
            var report = new AutoplayReport { Runs = runs };
            for (int seed = 1; seed <= runs; seed++)
            {
                var game = new Game(seed);
                var policy = new Random(seed * 7919);
                int guard = 0;
                while (!game.IsOver && guard++ < 500) Step(game, policy, report);
                if (!game.IsOver) report.Violations.Add($"seed {seed}: 게임이 끝나지 않음");
                report.TotalDays += game.Day;
                report.Endings.TryGetValue(game.Ending, out int count);
                report.Endings[game.Ending] = count + 1;
            }
            foreach (var definition in new Game().Events)
                if (!report.EventCounts.ContainsKey(definition.Id))
                    report.Violations.Add($"사건 {definition.Id}이(가) 한 번도 나오지 않음");
            return report;
        }

        static void Step(Game game, Random policy, AutoplayReport report)
        {
            var current = game.CurrentEvent;
            if (current != null)
            {
                report.EventCounts.TryGetValue(current.Id, out int count);
                report.EventCounts[current.Id] = count + 1;
                CheckEvent(game, current, report);
                var options = current.Options.Select((o, i) => (o, i)).Where(x => x.o.Available == null || x.o.Available(game)).ToList();
                if (options.Count == 0) { report.Violations.Add($"사건 {current.Id}: 고를 수 있는 선택지가 없음"); game.PendingEvents.Dequeue(); return; }
                game.Choose(options[policy.Next(options.Count)].i);
                return;
            }
            if (game.Phase == Phase.Day)
            {
                foreach (var person in game.People.Where(p => p.CanWork))
                    if (policy.Next(100) < 45) game.Assign(person, (Role)policy.Next(6));
                if (policy.Next(100) < 15) game.Ration = (Ration)policy.Next(3);
                if (policy.Next(100) < 10) game.SetShieldBoost(policy.Next(2) == 0);
                if (policy.Next(100) < 50)
                {
                    var type = new[] { BuildingType.Quarters, BuildingType.Clinic, BuildingType.Workshop, BuildingType.Wall, BuildingType.Farm }[policy.Next(5)];
                    game.Build(type, policy.Next(Game.GridSize), policy.Next(Game.GridSize));
                }
                game.EndDay();
                return;
            }
            if (game.Phase == Phase.Night)
            {
                if (game.RaidTonight)
                {
                    report.Raids++;
                    game.ResolveRaid((NightOrder)policy.Next(3));
                }
                else
                {
                    var activity = (IndoorActivity)policy.Next(4);
                    if (!game.CanDo(activity)) activity = IndoorActivity.Care;
                    game.DoIndoor(activity);
                }
            }
            foreach (var resource in game.Stock)
                if (resource.Value < 0) report.Violations.Add($"자원 {resource.Key}이(가) 음수");
        }

        static void CheckEvent(Game game, GameEvent current, AutoplayReport report)
        {
            if (!current.Major) return;
            if (!current.Foreshadows.Any(game.Flags.Contains))
                report.Violations.Add($"큰 사건 {current.Id}: 복선 없이 발생");
            if (!game.ChronicleFor(current.CauseTag).Any())
                report.Violations.Add($"큰 사건 {current.Id}: 원인이 된 선택 기록 없음");
        }
    }
}
