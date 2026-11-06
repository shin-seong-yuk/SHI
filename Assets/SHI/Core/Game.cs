using System;
using System.Collections.Generic;
using System.Linq;

namespace SHI.Core
{
    public sealed class Game
    {
        public const int GridSize = 12;
        public const int LastDay = 15;
        public const int MaxEventsPerDay = 2;

        public static readonly Dictionary<BuildingType, BuildingInfo> Catalog = new Dictionary<BuildingType, BuildingInfo>
        {
            { BuildingType.Shield, new BuildingInfo { Name = "전파 차폐기", Description = "PHS의 감시를 막는다", W = 2, D = 2 } },
            { BuildingType.Warehouse, new BuildingInfo { Name = "창고", Description = "자원과 약품을 보관한다", W = 3, D = 2 } },
            { BuildingType.Quarters, new BuildingInfo { Name = "숙소", Description = "4명이 머물 수 있다", W = 2, D = 2, ScrapCost = 6 } },
            { BuildingType.Clinic, new BuildingInfo { Name = "의무실", Description = "약품으로 부상자를 치료한다", W = 3, D = 2, ScrapCost = 8 } },
            { BuildingType.Workshop, new BuildingInfo { Name = "작업장", Description = "장비 제작을 열고 고철 수집을 늘린다", W = 3, D = 2, ScrapCost = 10, BatteryCost = 1 } },
            { BuildingType.Wall, new BuildingInfo { Name = "방벽", Description = "습격 방어력을 3 올린다", W = 2, D = 1, ScrapCost = 5 } },
            { BuildingType.Farm, new BuildingInfo { Name = "배양실", Description = "매일 식량 3을 만든다", W = 2, D = 3, ScrapCost = 7, BatteryCost = 1 } },
        };

        public readonly Random Rng;
        public readonly List<Person> People = new List<Person>();
        public readonly List<Building> Buildings = new List<Building>();
        public readonly List<ChronicleEntry> Chronicle = new List<ChronicleEntry>();
        public readonly List<string> Log = new List<string>();
        public readonly HashSet<string> Flags = new HashSet<string>();
        public readonly HashSet<string> FiredEvents = new HashSet<string>();
        public readonly Dictionary<Res, int> Stock = new Dictionary<Res, int>();
        public readonly List<GameEvent> Events;
        public readonly Queue<GameEvent> PendingEvents = new Queue<GameEvent>();

        public int Day = 1;
        public Phase Phase = Phase.Day;
        public EndReason Ending = EndReason.None;
        public int Morale = 60;
        public int Anger = 15;
        public int Detection = 10;
        public int Recovery = 34;
        public int Weapons;
        public int OvertimeCount;
        public int FoodYieldPenalty;
        public Ration Ration = Ration.Normal;
        public bool ShieldBoost;
        public bool BuiltToday;
        public bool DroneSeen;
        public bool RaidTonight;
        public bool ForcedRaid;
        public int DaysSinceRaid = 2;
        public RaidResult LastRaid;
        public int YunRefusedDay = -1;
        public int YunLedgerDay = -1;
        public int Accepted;
        public int Refused;

        public Game(int seed = 2327)
        {
            Rng = new Random(seed);
            Stock[Res.Scrap] = 12;
            Stock[Res.Battery] = 6;
            Stock[Res.Food] = 12;
            Stock[Res.Medicine] = 3;

            AddPerson("yun", "윤 씨", "창고지기", "장부를 꼼꼼히 쓴다. 아픈 딸이 있다.", Role.Store, 0, Temperaments.Get("family_first"));
            AddPerson("seojin", "서진", "탐사꾼", "폐허의 길을 잘 안다.", Role.Scrap, 1, Temperaments.PickFrom(Rng, "optimist", "opportunist", "survivor", "daredevil"));
            AddPerson("hanna", "한나", "간호사", "전쟁 전 병원에서 일했다. 남편 도현과 함께 왔다.", Role.Food, 0, Temperaments.PickFrom(Rng, "caregiver", "saving_face", "devoted"));
            AddPerson("dohyun", "도현", "전직 군인", "말수가 적고 부당한 일을 참지 못한다.", Role.Guard, 1, Temperaments.PickFrom(Rng, "principled", "soldier", "avenger", "whistleblower"));
            AddPerson("minjae", "민재", "수리공 견습생", "기지에서 가장 어리다.", Role.Battery, 0, Temperaments.PickFrom(Rng, "weak_will", "optimist", "coward", "climber"));

            Buildings.Add(new Building { Type = BuildingType.Shield, X = 5, Y = 5, W = 2, D = 2 });
            Buildings.Add(new Building { Type = BuildingType.Warehouse, X = 1, Y = 8, W = 3, D = 2 });
            Buildings.Add(new Building { Type = BuildingType.Quarters, X = 8, Y = 2, W = 2, D = 2 });
            RefreshExposure();

            Events = EventLibrary.Create();
            Write("2327년, 서울 폐허. 우리는 여기서 다시 시작한다.");
            RollRaid();
            QueueEvents();
        }

        public int ShieldRange => ShieldBoost ? 6 : 5;
        public int ShieldBatteryUse => Flags.Contains("oh_tuned") ? (ShieldBoost ? 1 : 0) : (ShieldBoost ? 2 : 1);
        public int FoodUse => Ration == Ration.Reduced ? (Living + 1) / 2 : Ration == Ration.Generous ? Living + (Living + 1) / 2 : Living;
        public int Living => People.Count(p => p.Present);
        public int Guards => People.Count(p => p.CanWork && p.Role == Role.Guard);
        public int WallCount => Buildings.Count(b => b.Type == BuildingType.Wall);
        public bool Has(BuildingType type) => Buildings.Any(b => b.Type == type);
        public Person Find(string id) => People.FirstOrDefault(p => p.Id == id);
        public bool IsPresent(string id) { var p = Find(id); return p != null && p.Present; }
        public bool HasMemory(string id, string memory) { var p = Find(id); return p != null && p.Memory.Contains(memory); }
        public void Remember(string id, string memory) { Find(id)?.Memory.Add(memory); }
        public bool IsOver => Phase == Phase.End;
        public GameEvent CurrentEvent => PendingEvents.Count > 0 ? PendingEvents.Peek() : null;

        public Person AddPerson(string id, string name, string title, string note, Role role, int skill, Temperament temper, bool frail = false)
        {
            var person = new Person { Id = id, Name = name, Title = title, Note = note, Role = role, Skill = skill, Frail = frail, Temper = temper, JoinedDay = Day };
            person.Trust += Rng.Next(-5, 6);
            People.Add(person);
            return person;
        }

        public void Add(Res res, int amount) { Stock[res] = Math.Max(0, Stock[res] + amount); }
        public void ChangeMorale(int amount) { Morale = Clamp(Morale + amount); }
        public void ChangeAnger(int amount)
        {
            Anger = Clamp(Anger + amount);
            if (amount <= 0) return;
            foreach (var p in People.Where(o => o.Present)) p.ChangeGrievance(amount / 3 + Math.Max(0, p.Temper.Grudge));
        }
        public void ChangeDetection(int amount) { Detection = Clamp(Detection + amount); }
        static int Clamp(int value) => Math.Max(0, Math.Min(100, value));

        public void Write(string text) { Log.Add(text); }
        public void Record(string text, string tag) { Chronicle.Add(new ChronicleEntry { Day = Day, Text = text, Tag = tag }); }
        public IEnumerable<ChronicleEntry> ChronicleFor(string tag) => Chronicle.Where(c => c.Tag == tag);

        public bool Assign(Person person, Role role)
        {
            if (Phase != Phase.Day || person == null || !person.CanWork) return false;
            person.Role = role;
            return true;
        }

        public bool CanAfford(BuildingType type)
        {
            var info = Catalog[type];
            return Stock[Res.Scrap] >= info.ScrapCost && Stock[Res.Battery] >= info.BatteryCost;
        }

        public bool CanPlace(BuildingType type, int x, int y)
        {
            var info = Catalog[type];
            if (x < 0 || y < 0 || x + info.W > GridSize || y + info.D > GridSize) return false;
            return !Buildings.Any(b => x < b.X + b.W && x + info.W > b.X && y < b.Y + b.D && y + info.D > b.Y);
        }

        public bool Build(BuildingType type, int x, int y)
        {
            if (Phase != Phase.Day || BuiltToday || CurrentEvent != null) return false;
            if (type == BuildingType.Shield || type == BuildingType.Warehouse) return false;
            if (!CanAfford(type) || !CanPlace(type, x, y)) return false;
            var info = Catalog[type];
            Add(Res.Scrap, -info.ScrapCost);
            Add(Res.Battery, -info.BatteryCost);
            Buildings.Add(new Building { Type = type, X = x, Y = y, W = info.W, D = info.D });
            BuiltToday = true;
            RefreshExposure();
            Write(Ko.Eul(info.Name) + " 지었다.");
            return true;
        }

        public void RefreshExposure()
        {
            var center = Buildings.First(b => b.Type == BuildingType.Shield);
            double cx = center.X + center.W / 2.0, cy = center.Y + center.D / 2.0;
            foreach (var b in Buildings)
            {
                double dx = b.X + b.W / 2.0 - cx, dy = b.Y + b.D / 2.0 - cy;
                b.Exposed = Math.Sqrt(dx * dx + dy * dy) > ShieldRange;
            }
        }

        public void SetShieldBoost(bool on)
        {
            if (Phase != Phase.Day) return;
            ShieldBoost = on;
            RefreshExposure();
        }

        public int YieldOf(Role role)
        {
            int workers = 0, total = 0;
            foreach (var p in People.Where(p => p.CanWork && p.Role == role))
            {
                workers++;
                total += p.Skill;
            }
            if (workers == 0) return 0;
            switch (role)
            {
                case Role.Scrap: return workers * (Has(BuildingType.Workshop) ? 4 : 3) + total;
                case Role.Food: return Math.Max(workers, workers * (3 - FoodYieldPenalty) + total);
                case Role.Battery: return workers * 2 + total;
                default: return 0;
            }
        }

        public void EndDay()
        {
            if (Phase != Phase.Day || CurrentEvent != null) return;
            int scrap = YieldOf(Role.Scrap), food = YieldOf(Role.Food) + (Has(BuildingType.Farm) ? 3 * Buildings.Count(b => b.Type == BuildingType.Farm) : 0), battery = YieldOf(Role.Battery);
            Add(Res.Scrap, scrap);
            Add(Res.Food, food);
            Add(Res.Battery, battery);
            Write($"수확: 고철 +{scrap}, 식량 +{food}, 폐배터리 +{battery}");
            if (!People.Any(p => p.CanWork && p.Role == Role.Store) && Stock[Res.Food] > 0)
            {
                Add(Res.Food, -1);
                Write("창고를 지키는 사람이 없어 식량 일부가 상했다.");
            }
            RaidTonight = ForcedRaid || RaidTonight;
            Phase = Phase.Night;
            Write(RaidTonight ? "어둠 속에서 붉은 눈들이 다가온다." : "오늘 밤은 조용하다.");
        }

        public bool CanDo(IndoorActivity activity)
        {
            if (activity == IndoorActivity.Craft) return Has(BuildingType.Workshop) && Stock[Res.Scrap] >= 4;
            return true;
        }

        public void DoIndoor(IndoorActivity activity)
        {
            if (Phase != Phase.Night || RaidTonight || !CanDo(activity)) return;
            switch (activity)
            {
                case IndoorActivity.Craft:
                    Add(Res.Scrap, -4);
                    Weapons++;
                    Write("경비대의 장비를 만들었다. 방어력이 올랐다.");
                    break;
                case IndoorActivity.Train:
                    var trainees = People.Where(p => p.CanWork && p.Role == Role.Guard).ToList();
                    if (trainees.Count == 0) trainees = People.Where(p => p.CanWork).Take(1).ToList();
                    foreach (var p in trainees) p.Skill = Math.Min(4, p.Skill + 1);
                    Write(trainees.Count > 0 ? Ko.I(string.Join(", ", trainees.Select(p => p.Name))) + " 수련했다." : "수련할 사람이 없었다.");
                    break;
                case IndoorActivity.Care:
                    ChangeMorale(6);
                    ChangeAnger(-3);
                    foreach (var p in People.Where(o => o.Present)) { p.ChangeGrievance(-4); p.ChangeTrust(1 + Math.Max(0, p.Temper.Empathy) / 2); }
                    Write("밤늦게까지 사람들의 이야기를 들었다.");
                    break;
                case IndoorActivity.Overtime:
                    Add(Res.Scrap, 4);
                    Add(Res.Food, 2);
                    ChangeMorale(-3);
                    ChangeAnger(6);
                    OvertimeCount++;
                    foreach (var p in People.Where(o => o.CanWork)) p.ChangeGrievance(4 + Math.Max(0, p.Temper.Self));
                    Record($"{Day}일차 밤에 야간 특근을 지시했다.", "overtime");
                    Write("야간 특근으로 자원을 더 모았다. 사람들이 지쳐 보인다.");
                    break;
            }
            FinishNight();
        }

        public int DefensePower(NightOrder order)
        {
            int power = WallCount * 3;
            foreach (var p in People.Where(p => p.CanWork && p.Role == Role.Guard)) power += 2 + p.Skill + Math.Min(Weapons, 3);
            if (order == NightOrder.Ambush) power += 2;
            if (order == NightOrder.Overload && Stock[Res.Battery] >= 2) power += 3;
            return power;
        }

        public int AttackPower => 4 + Detection / 12 + Day / 4;

        public RaidResult ResolveRaid(NightOrder order)
        {
            if (Phase != Phase.Night || !RaidTonight) return null;
            if (order == NightOrder.Overload) Add(Res.Battery, -2);
            int attack = AttackPower + Rng.Next(-2, 3), defense = DefensePower(order);
            string fledText = "";
            foreach (var guard in People.Where(p => p.CanWork && p.Role == Role.Guard).ToList())
            {
                var flee = new Chance(3).Trait("용기", guard.Temper.Courage, -6).Trait("자기보호", guard.Temper.Self, 3).Add("불만", guard.Grievance / 10).Add("신뢰", (50 - guard.Trust) / 10);
                if (!flee.Roll(Rng)) continue;
                defense -= 2 + guard.Skill + Math.Min(Weapons, 3);
                guard.ChangeTrust(-10);
                guard.Diary.Add($"{Day}일차: 습격 중에 도망쳤다.");
                ChangeAnger(4);
                fledText += $" {Ko.I(guard.Name)} 겁에 질려 자리를 떠났다.";
                Record($"{Day}일차: {Ko.I(guard.Name)} 습격 중 자리를 떠났다. 이유 {flee.Explain()}", guard.Tag);
            }
            int margin = defense - attack;
            var result = new RaidResult { Won = margin >= 0, Attack = attack, Defense = defense };
            var guards = People.Where(p => p.CanWork && p.Role == Role.Guard).ToList();
            var targets = guards.Count > 0 ? guards : People.Where(p => p.Health == Health.Healthy).ToList();

            if (result.Won)
            {
                ChangeMorale(4);
                int risk = order == NightOrder.Ambush ? 40 : 20;
                if (targets.Count > 0 && Rng.Next(100) < risk)
                {
                    Injure(targets[Rng.Next(targets.Count)]);
                    result.Summary = "습격을 막았지만 다친 사람이 나왔다.";
                }
                else result.Summary = "습격을 막아냈다.";
                ChangeDetection(-3);
            }
            else
            {
                Add(Res.Food, -3);
                Add(Res.Scrap, -3);
                ChangeMorale(-12);
                ChangeAnger(5);
                int injuries = Math.Min(targets.Count, margin <= -4 ? 2 : 1);
                string heroText = "";
                for (int i = 0; i < injuries; i++)
                {
                    var target = targets[i];
                    var hero = People.Where(p => p != target && p.Health == Health.Healthy && !p.Frail && !targets.Take(injuries).Contains(p)).OrderByDescending(p => p.Temper.Courage + p.Temper.Group).FirstOrDefault();
                    if (hero != null)
                    {
                        var shield = new Chance(0).Trait("용기", hero.Temper.Courage, 5).Trait("공동체", hero.Temper.Group, 5).Trait("연민", hero.Temper.Empathy, 2).Add("신뢰", (hero.Trust - 50) / 5);
                        if (shield.Roll(Rng))
                        {
                            Injure(hero);
                            hero.ChangeTrust(5);
                            target.ChangeTrust(8);
                            ChangeMorale(4);
                            heroText += $" {Ko.I(hero.Name)} {target.Name} 대신 공격을 맞았다.";
                            Record($"{Day}일차: {Ko.I(hero.Name)} {target.Name} 대신 공격을 맞았다. 이유 {shield.Explain()}", hero.Tag);
                            continue;
                        }
                    }
                    Injure(target);
                }
                result.Summary = "방어선이 무너져 자원을 빼앗기고 사람들이 다쳤다." + heroText;
                if (margin <= -8 && targets.Count > 0)
                {
                    var victim = targets[Rng.Next(targets.Count)];
                    victim.Health = Health.Dead;
                    ChangeMorale(-10);
                    Record($"{Ko.I(victim.Name)} {Day}일차 습격에서 죽었다.", "death");
                    result.Summary += $" {Ko.Eun(victim.Name)} 돌아오지 못했다.";
                }
            }
            result.Summary += fledText;
            Write(result.Summary);
            Record($"{Day}일차 습격: {result.Summary}", "raid");
            LastRaid = result;
            ForcedRaid = false;
            DaysSinceRaid = 0;
            FinishNight();
            return result;
        }

        void Injure(Person person)
        {
            if (person.Health != Health.Healthy) return;
            person.Health = Health.Injured;
            person.InjuredDays = 0;
            Record($"{Ko.I(person.Name)} {Day}일차에 다쳤다.", "injury");
        }

        public void InjurePerson(Person person) => Injure(person);

        void FinishNight()
        {
            if (Morale <= 0 || Anger >= 100 || Living <= 1) { CheckCollapse(); if (IsOver) return; }
            Day++;
            DaysSinceRaid++;
            BuiltToday = false;
            Morning();
        }

        void Morning()
        {
            Phase = Phase.Day;
            int food = FoodUse;
            bool hungry = Stock[Res.Food] < food;
            if (!hungry)
            {
                Add(Res.Food, -food);
                if (Ration == Ration.Reduced) { ChangeMorale(-3); ChangeAnger(2); }
                if (Ration == Ration.Generous) { ChangeMorale(3); ChangeAnger(-1); }
            }
            else
            {
                Stock[Res.Food] = 0;
                ChangeMorale(-10);
                ChangeAnger(7);
                Flags.Add("starved");
                Write("식량이 모자라 굶은 사람이 나왔다.");
                Record($"{Day}일차 아침에 식량이 모자랐다.", "hunger");
            }

            int use = ShieldBatteryUse;
            if (Stock[Res.Battery] >= use)
            {
                Add(Res.Battery, -use);
                ChangeDetection(ShieldBoost ? 0 : 2);
            }
            else
            {
                ChangeDetection(10);
                Write("폐배터리가 떨어져 차폐기가 멈췄다. 탐지도가 크게 올랐다.");
                Record($"{Day}일차에 차폐기가 멈췄다.", "shield");
            }
            int exposed = Buildings.Count(b => b.Exposed);
            if (exposed > 0)
            {
                ChangeDetection(exposed * 2);
                Write($"차폐 범위 밖 건물 {exposed}채가 감시에 노출되고 있다.");
            }

            int capacity = Buildings.Count(b => b.Type == BuildingType.Quarters) * 4;
            if (Living > capacity) ChangeMorale(-2);
            if (People.Any(p => p.CanWork && p.Role == Role.Rest)) ChangeMorale(1);
            if (Anger > 0 && Ration != Ration.Reduced && OvertimeCount == 0) ChangeAnger(-1);
            Recovery = Math.Min(100, Recovery + 1);

            TreatInjured();
            UpdatePeople(hungry);
            CheckCollapse();
            if (IsOver) return;
            if (Day > LastDay)
            {
                Ending = EndReason.Survived;
                Phase = Phase.End;
                return;
            }
            RollRaid();
            QueueEvents();
        }

        void UpdatePeople(bool hungry)
        {
            int mood = 0;
            foreach (var p in People.Where(o => o.Present))
            {
                mood += p.Temper.Mood;
                if (Ration == Ration.Reduced) p.ChangeGrievance(2 + Math.Max(0, p.Temper.Self));
                if (Ration == Ration.Generous) p.ChangeGrievance(-1);
                if (hungry) p.ChangeGrievance(4 + Math.Max(0, p.Temper.Self));
                if (p.CanWork && p.Role == Role.Rest) p.ChangeGrievance(-2);
                if (p.Health == Health.Injured && Stock[Res.Medicine] == 0) p.ChangeGrievance(3);
                if (p.Grievance >= 60) p.ChangeTrust(-1);
                if (!p.TemperKnown && Day >= p.JoinedDay + 2)
                {
                    p.TemperKnown = true;
                    Write($"함께 지내며 {p.Name}의 성향을 알게 됐다. {p.Temper.Name}.");
                }
                foreach (var old in p.Signs.Where(s => Day - s.Value >= 6).Select(s => s.Key).ToList()) p.Signs.Remove(old);
            }
            if (mood >= 2) ChangeMorale(1);
            if (mood <= -2) ChangeMorale(-1);
        }

        void TreatInjured()
        {
            foreach (var p in People.Where(p => p.Health == Health.Injured).ToList())
            {
                p.InjuredDays++;
                if (Has(BuildingType.Clinic) && Stock[Res.Medicine] > 0)
                {
                    Add(Res.Medicine, -1);
                    p.Health = Health.Healthy;
                    Write($"{Ko.I(p.Name)} 의무실에서 치료받았다.");
                }
                else if (p.InjuredDays >= 3 && Stock[Res.Medicine] > 0)
                {
                    Add(Res.Medicine, -1);
                    p.Health = Health.Healthy;
                    Write($"{Ko.I(p.Name)} 약을 쓰고 회복했다.");
                }
                else if (p.InjuredDays >= 4 && Rng.Next(100) < 40)
                {
                    p.Health = Health.Dead;
                    ChangeMorale(-10);
                    Write($"{Ko.I(p.Name)} 약 없이 버티다 숨졌다.");
                    Record($"{Ko.I(p.Name)} {Day}일차에 치료받지 못하고 숨졌다.", "death");
                }
            }
        }

        void RollRaid()
        {
            bool canRaid = Day >= 3 && DaysSinceRaid >= 2;
            RaidTonight = ForcedRaid || (canRaid && Rng.Next(100) < 10 + Detection * 3 / 4);
            DroneSeen = RaidTonight || (canRaid && Rng.Next(100) < 15);
            if (DroneSeen) Write("감시 드론이 기지 위를 지나갔다. 오늘 밤 습격이 올 수도 있다.");
        }

        void QueueEvents()
        {
            var candidates = new List<GameEvent>();
            foreach (var e in Events.Where(e => !FiredEvents.Contains(e.Id) && e.Condition(this)))
            {
                var odds = e.Odds?.Invoke(this);
                if (odds != null && !odds.Roll(Rng)) continue;
                e.Reason = odds?.Explain();
                candidates.Add(e);
            }
            if (Day >= 2) candidates.AddRange(NpcBehaviors.Roll(this));
            if (Day >= 3 && Living < 10 && EventLibrary.HasStrangers(this))
            {
                var arrive = new Chance(10).Add("사기", Morale / 6).Add("탐지 위험", -Detection / 12).Add("돌려보낸 소문", -Refused * 3);
                if (arrive.Roll(Rng))
                {
                    var stranger = EventLibrary.MakeStranger(this);
                    stranger.Reason = arrive.Explain();
                    candidates.Add(stranger);
                }
            }

            var speakers = new HashSet<string>();
            int taken = 0;
            foreach (var e in candidates.OrderByDescending(e => e.Major).ThenByDescending(e => e.Priority))
            {
                if (taken >= MaxEventsPerDay) break;
                if (e.SpeakerId != null && !speakers.Add(e.SpeakerId)) continue;
                taken++;
                FiredEvents.Add(e.Id);
                e.OnQueue?.Invoke(this);
                PendingEvents.Enqueue(e);
            }
        }

        public void Choose(int optionIndex)
        {
            var current = CurrentEvent;
            if (current == null) return;
            var option = current.Options[optionIndex];
            if (option.Available != null && !option.Available(this)) return;
            PendingEvents.Dequeue();
            option.Apply?.Invoke(this);
            if (!string.IsNullOrEmpty(option.Chronicle)) Record($"{Day}일차: {option.Chronicle}", option.Tag ?? current.CauseTag ?? current.Id);
            RefreshExposure();
            CheckCollapse();
        }

        void CheckCollapse()
        {
            if (IsOver) return;
            EndReason reason = Living <= 1 ? EndReason.NoPeople : Anger >= 100 ? EndReason.Revolt : Morale <= 0 ? EndReason.MoraleLost : EndReason.None;
            if (reason == EndReason.None) return;
            Ending = reason;
            Phase = Phase.End;
            PendingEvents.Clear();
        }
    }
}
