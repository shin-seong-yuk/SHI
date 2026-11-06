using System;
using System.Linq;
using SHI.Core;

public static class Program
{
    public static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var report = Autoplayer.Run(5000);
        Console.WriteLine($"[random] runs={report.Runs} avgDays={(double)report.TotalDays / report.Runs:F1} raids/run={(double)report.Raids / report.Runs:F2} events/day={(double)report.TotalEvents / report.TotalDays:F2}");
        foreach (var ending in report.Endings.OrderBy(e => e.Key)) Console.WriteLine($"  ending {ending.Key}: {ending.Value * 100.0 / report.Runs:F1}%");
        foreach (var e in report.EventCounts.OrderByDescending(e => e.Value)) Console.WriteLine($"  event {e.Key}: {e.Value * 100.0 / report.Runs:F1}%");
        foreach (var e in report.TemperamentEvents.OrderByDescending(e => e.Value).Take(10)) Console.WriteLine($"  acts by {e.Key}: {e.Value}");
        var violations = report.Violations.Distinct().ToList();
        Console.WriteLine($"violations={report.Violations.Count} distinct={violations.Count}");
        foreach (var v in violations.Take(20)) Console.WriteLine("  " + v);

        foreach (var refuse in new[] { false, true })
        {
            int survived = 0, betrayed = 0, recruits = 0, lost = 0, totalMorale = 0, totalDetection = 0, totalRaids = 0;
            const int runs = 2000;
            for (int seed = 1; seed <= runs; seed++)
            {
                var g = PlaySensibly(seed, refuse);
                if (g.Ending == EndReason.Survived) survived++;
                if (g.Flags.Contains("yun_betrayed")) betrayed++;
                recruits += g.Accepted;
                lost += g.People.Count(p => p.Health == Health.Gone || p.Health == Health.Dead);
                totalMorale += g.Morale;
                totalDetection += g.Detection;
                totalRaids += g.Chronicle.Count(c => c.Tag == "raid");
            }
            Console.WriteLine($"[sensible refuse={refuse}] survived={survived * 100.0 / runs:F1}% betrayed={betrayed * 100.0 / runs:F1}% avgMorale={totalMorale / runs} avgDetection={totalDetection / runs} raids={(double)totalRaids / runs:F1} recruits={(double)recruits / runs:F1} lost={(double)lost / runs:F1}");
        }
        return violations.Count == 0 ? 0 : 1;
    }

    static Game PlaySensibly(int seed, bool refuseYun)
    {
        var g = new Game(seed);
        var spots = new[] { (BuildingType.Wall, 9, 6), (BuildingType.Clinic, 6, 9), (BuildingType.Wall, 2, 5), (BuildingType.Workshop, 9, 9), (BuildingType.Farm, 3, 2) };
        int nextBuild = 0, guard = 0;
        while (!g.IsOver && guard++ < 500)
        {
            var e = g.CurrentEvent;
            if (e != null)
            {
                int pick = 0;
                if (e.Id == "yun_medicine" && refuseYun) pick = 2;
                if (e.Id == "yun_ledger" && refuseYun) pick = 1;
                if (e.Options[pick].Available != null && !e.Options[pick].Available(g))
                    pick = Array.FindIndex(e.Options, o => o.Available == null || o.Available(g));
                g.Choose(pick);
                continue;
            }
            if (g.Phase == Phase.Day)
            {
                var workers = g.People.Where(p => p.CanWork).ToList();
                int food = g.Stock[Res.Food] < g.FoodUse * 3 ? 2 : 1, guards = g.Detection > 35 ? 2 : 1;
                foreach (var p in workers)
                {
                    Role role = Role.Scrap;
                    if (p.Id == "yun") role = Role.Store;
                    else if (guards > 0) { role = Role.Guard; guards--; }
                    else if (food > 0) { role = Role.Food; food--; }
                    else if (g.Stock[Res.Battery] < 3) role = Role.Battery;
                    g.Assign(p, role);
                }
                if (nextBuild < spots.Length && g.Build(spots[nextBuild].Item1, spots[nextBuild].Item2, spots[nextBuild].Item3)) nextBuild++;
                g.EndDay();
            }
            else if (g.RaidTonight) g.ResolveRaid(NightOrder.HoldWalls);
            else g.DoIndoor(g.Morale < 55 ? IndoorActivity.Care : g.Guards > 0 ? IndoorActivity.Train : IndoorActivity.Care);
        }
        return g;
    }
}
