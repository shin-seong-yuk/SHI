using System;
using System.Collections.Generic;
using System.Linq;

namespace SHI.Core
{
    // Something a person may do on their own. Harmful acts show a sign first, so the player can notice.
    public sealed class NpcBehavior
    {
        public string Id;
        public string Name;
        public bool Harmful;
        public bool Major;
        public int Priority = 30;
        public string SignTitle;
        public Func<Game, Person, string> SignBody;
        public string ActTitle;
        public Func<Game, Person, bool> Gate;
        public Func<Game, Person, Chance> Odds;
        public Func<Game, Person, string> Act;
    }

    public static class NpcBehaviors
    {
        static string N(Person p) => p.Name;

        public static readonly List<NpcBehavior> All = new List<NpcBehavior>
        {
            new NpcBehavior
            {
                Id = "steal_food", Name = "식량 횡령", Harmful = true, SignTitle = "맞지 않는 식량 수",
                SignBody = (g, p) => $"요즘 {Ko.I(N(p))} 밤에 식량 더미 근처를 서성인다는 말이 돌아요. 아직 없어진 것은 많지 않아요.",
                ActTitle = "사라진 식량",
                Gate = (g, p) => g.Stock[Res.Food] > 2,
                Odds = (g, p) => Situation(g, p, 1).Trait("탐욕", p.Temper.Greed, 4).Trait("충동", p.Temper.Impulse, 2).Trait("정직", p.Temper.Honesty, -3).Add("굶주림", g.Flags.Contains("starved") ? 10 : 0).Add("창고 담당", p.Role == Role.Store ? 4 : 0),
                Act = (g, p) => { int n = Math.Min(g.Stock[Res.Food], 4 + Math.Max(0, p.Temper.Greed)); g.Add(Res.Food, -n); return $"식량 {n}이 사라졌어요. 흔적은 {N(p)}의 잠자리까지 이어져 있어요."; },
            },
            new NpcBehavior
            {
                Id = "sell_medicine", Name = "약품 밀매", Harmful = true, SignTitle = "낯선 거래 흔적",
                SignBody = (g, p) => $"{Ko.I(N(p))} 기지 밖 떠돌이들과 무언가를 흥정하는 모습을 봤어요. 약품 수량을 묻고 다닌다고 해요.",
                ActTitle = "밀매된 약품",
                Gate = (g, p) => g.Stock[Res.Medicine] > 0 && p.Temper.Greed >= 1,
                Odds = (g, p) => Situation(g, p, 0).Trait("탐욕", p.Temper.Greed, 5).Trait("연민", p.Temper.Empathy, -2).Trait("정직", p.Temper.Honesty, -2),
                Act = (g, p) => { int n = Math.Min(g.Stock[Res.Medicine], 2); g.Add(Res.Medicine, -n); g.Add(Res.Battery, 1); return $"약품 {n}개가 없어졌어요. {Ko.I(N(p))} 떠돌이들에게 몰래 팔아넘겼어요."; },
            },
            new NpcBehavior
            {
                Id = "inform_phs", Name = "PHS 밀고", Harmful = true, Major = true, Priority = 90, SignTitle = "수상한 신호",
                SignBody = (g, p) => $"새벽에 {Ko.I(N(p))} 기지 끝에서 낡은 무전기를 만지는 걸 봤어요. 고장 난 걸 고치는 중이라고 하던데요.",
                ActTitle = "새어 나간 위치",
                Gate = (g, p) => p.Temper.Loyalty <= 0 && p.Trust < 45 && g.Day >= 5,
                Odds = (g, p) => Situation(g, p, 0, 4).Trait("충성", p.Temper.Loyalty, -3).Trait("자기보호", p.Temper.Self, 2).Trait("탐욕", p.Temper.Greed, 2),
                Act = (g, p) => { g.ChangeDetection(30); g.ForcedRaid = true; g.RaidTonight = true; g.DroneSeen = true; p.Health = Health.Gone; return $"{Ko.I(N(p))} 기지 위치를 PHS에 넘기고 사라졌어요. 오늘 밤 습격이 올 거예요."; },
            },
            new NpcBehavior
            {
                Id = "desert", Name = "탈영", Harmful = true, Priority = 50, SignTitle = "꾸려진 짐",
                SignBody = (g, p) => $"{N(p)}의 잠자리에 짐이 꾸려져 있어요. 요즘 기지 밖의 길을 자꾸 물어본대요.",
                ActTitle = "빈 잠자리",
                Gate = (g, p) => p.Health == Health.Healthy && (g.Morale < 55 || g.Detection > 50),
                Odds = (g, p) => Situation(g, p, 0).Trait("자기보호", p.Temper.Self, 3).Trait("용기", p.Temper.Courage, -3).Add("희망 없음", Math.Max(0, 55 - g.Morale) / 4).Add("위험 고조", Math.Max(0, g.Detection - 50) / 6),
                Act = (g, p) => { int n = Math.Min(g.Stock[Res.Food], 2); g.Add(Res.Food, -n); p.Health = Health.Gone; g.ChangeMorale(-5); return $"{Ko.I(N(p))} 밤사이 식량 {n}을 들고 기지를 떠났어요."; },
            },
            new NpcBehavior
            {
                Id = "sabotage", Name = "태업", Harmful = true, SignTitle = "느려진 손",
                SignBody = (g, p) => $"{Ko.I(N(p))} 일을 맡고도 쉬는 시간이 부쩍 늘었어요. 불만이 있다는 말도 들려요.",
                ActTitle = "멈춘 작업",
                Gate = (g, p) => p.Grievance >= 25,
                Odds = (g, p) => Situation(g, p, 2).Trait("앙심", p.Temper.Grudge, 3).Trait("공동체", p.Temper.Group, -3),
                Act = (g, p) => { g.Add(Res.Scrap, -3); g.ChangeMorale(-2); return $"{Ko.I(N(p))} 일부러 손을 놓아 고철 작업이 망가졌어요."; },
            },
            new NpcBehavior
            {
                Id = "incite", Name = "선동", Harmful = true, Priority = 45, SignTitle = "웅성거림",
                SignBody = (g, p) => $"밤마다 {N(p)} 주변에 사람들이 모여요. 지도부 이야기를 나누다 제가 가면 입을 닫아요.",
                ActTitle = "퍼져 나간 불만",
                Gate = (g, p) => g.Anger >= 20 || p.Grievance >= 35,
                Odds = (g, p) => Situation(g, p, 0).Trait("명예", p.Temper.Pride, 2).Trait("충성", p.Temper.Loyalty, -3).Trait("용기", p.Temper.Courage, 1),
                Act = (g, p) => { g.ChangeAnger(10); g.ChangeMorale(-3); return $"{Ko.I(N(p))} 공개적으로 지도부를 비난했고, 고개를 끄덕이는 사람이 늘었어요."; },
            },
            new NpcBehavior
            {
                Id = "indulge", Name = "욕망에 무너짐", Harmful = true, SignTitle = "떨리는 다짐",
                SignBody = (g, p) => $"{Ko.I(N(p))} 저를 붙잡고 다시는 안 그러겠다고 몇 번이나 말했어요. 묻지도 않았는데요.",
                ActTitle = "참지 못한 밤",
                Gate = (g, p) => p.Temper.Impulse >= 1,
                Odds = (g, p) => new Chance(2).Trait("충동", p.Temper.Impulse, 5).Add("스트레스", p.Grievance / 10).Add("신뢰", (50 - p.Trust) / 15),
                Act = (g, p) => { g.Add(Res.Medicine, g.Stock[Res.Medicine] > 0 ? -1 : 0); g.Add(Res.Food, -Math.Min(2, g.Stock[Res.Food])); g.InjurePerson(p); return $"{Ko.I(N(p))} 욕망을 참지 못하고 보급품에 손을 댔어요. 취해서 넘어져 다치기까지 했어요."; },
            },
            new NpcBehavior
            {
                Id = "pride_lash", Name = "명예를 지키려는 배신", Harmful = true, Priority = 60, SignTitle = "굳은 얼굴",
                SignBody = (g, p) => $"그 일 이후로 {Ko.I(N(p))} 사람들 앞에 나서지 않아요. 누가 자기 이야기를 하는지 자꾸 묻는대요.",
                ActTitle = "떠넘겨진 책임",
                Gate = (g, p) => p.Ashamed && p.Temper.Pride >= 1,
                Odds = (g, p) => new Chance(6).Trait("명예", p.Temper.Pride, 8).Add("수치", 10).Add("앙심", p.Temper.Grudge * 2),
                Act = (g, p) => { g.ChangeAnger(6); g.Add(Res.Scrap, -Math.Min(4, g.Stock[Res.Scrap])); p.Ashamed = false; return $"{Ko.I(N(p))} 자기 평판을 지키려고 장부를 고쳐 잘못을 다른 사람에게 떠넘겼어요. 고철 일부도 사라졌어요."; },
            },
            new NpcBehavior
            {
                Id = "fight", Name = "동료와 충돌", Harmful = true, SignTitle = "날 선 말들",
                SignBody = (g, p) => $"{Ko.I(N(p))} 식사 자리에서 동료에게 시비를 걸었어요. 오늘은 말렸지만 다음엔 모르겠어요.",
                ActTitle = "주먹다짐",
                Gate = (g, p) => g.People.Count(o => o.Health == Health.Healthy) >= 3,
                Odds = (g, p) => new Chance(1).Trait("앙심", p.Temper.Grudge, 3).Trait("충동", p.Temper.Impulse, 3).Add("불만", p.Grievance / 12),
                Act = (g, p) => { var others = g.People.Where(o => o != p && o.Health == Health.Healthy && !o.Frail).ToList(); var other = others.Count > 0 ? others[g.Rng.Next(others.Count)] : null; if (other != null) { g.InjurePerson(other); other.ChangeGrievance(10); other.Diary.Add($"{g.Day}일차: {N(p)}에게 맞아 다쳤다."); } g.ChangeMorale(-3); return other != null ? $"{Ko.I(N(p))} {N(other)}에게 주먹을 휘둘러 다치게 했어요." : $"{Ko.I(N(p))} 혼자 분을 못 이겨 물건을 부쉈어요."; },
            },
            new NpcBehavior
            {
                Id = "volunteer", Name = "자원 야근", Priority = 20, ActTitle = "밤새 쌓인 고철",
                Gate = (g, p) => p.CanWork,
                Odds = (g, p) => new Chance(1).Trait("공동체", p.Temper.Group, 4).Add("신뢰", (p.Trust - 50) / 8).Add("불만", -p.Grievance / 10),
                Act = (g, p) => { g.Add(Res.Scrap, 3); g.ChangeMorale(2); return $"{Ko.I(N(p))} 아무 말 없이 밤새 고철을 모아 왔어요."; },
            },
            new NpcBehavior
            {
                Id = "share_food", Name = "자기 몫 나눔", Priority = 20, ActTitle = "나눠 먹은 밥",
                Gate = (g, p) => g.Ration == Ration.Reduced || g.Flags.Contains("starved") || g.People.Any(o => o.Health == Health.Injured),
                Odds = (g, p) => new Chance(1).Trait("연민", p.Temper.Empathy, 5).Trait("자기보호", p.Temper.Self, -2).Add("신뢰", (p.Trust - 50) / 10),
                Act = (g, p) => { g.ChangeMorale(5); g.ChangeAnger(-3); return $"{Ko.I(N(p))} 자기 몫을 다친 사람과 아이들에게 나눠줬어요. 분위기가 조금 풀렸어요."; },
            },
            new NpcBehavior
            {
                Id = "reveal_sign", Name = "내부 고발", Priority = 40, ActTitle = "내부 고발",
                Gate = (g, p) => g.People.Any(o => o != p && o.Present && o.Signs.Count > 0),
                Odds = (g, p) => new Chance(-2).Trait("정직", p.Temper.Honesty, 6).Trait("용기", p.Temper.Courage, 3).Add("신뢰", (p.Trust - 50) / 5),
                Act = (g, p) => { var suspect = g.People.FirstOrDefault(o => o != p && o.Present && o.Signs.Count > 0); if (suspect == null) { g.ChangeMorale(1); return $"{Ko.I(N(p))} 기지 안의 수상한 일을 꼼꼼히 살펴 보고했어요."; } suspect.WatchedUntil = g.Day + 3; return $"{Ko.I(N(p))} 찾아와 {N(suspect)}의 수상한 행동을 알렸어요. 며칠간 {Ko.Eul(N(suspect))} 지켜보게 됐어요."; },
            },
        };

        // Common situational terms: complaint and trust always count.
        static Chance Situation(Game g, Person p, int baseChance, int weight = 6)
        {
            return new Chance(baseChance).Add("불만", p.Grievance / weight).Add("신뢰", (50 - p.Trust) / 8).Add("배급 삭감", g.Ration == Ration.Reduced ? 3 : 0);
        }

        public static string Tendency(int percent) => percent >= 25 ? "매우 높음" : percent >= 12 ? "높음" : percent >= 5 ? "보통" : "낮음";

        // Rolls every person's behaviors for this morning and returns ready-made events.
        public static List<GameEvent> Roll(Game g)
        {
            var result = new List<GameEvent>();
            foreach (var p in g.People.Where(o => o.Present))
                foreach (var b in All)
                {
                    if (p.Cooldowns.TryGetValue(b.Id, out int until) && until > g.Day) continue;
                    if (!b.Gate(g, p)) continue;
                    bool hasSign = p.Signs.TryGetValue(b.Id, out int signDay);
                    if (b.Harmful && hasSign && signDay >= g.Day) continue;
                    var odds = b.Odds(g, p);
                    if (b.Harmful && !hasSign)
                    {
                        if (p.Signs.Count == 0 && odds.Roll(g.Rng)) result.Add(MakeSign(p, b, odds));
                    }
                    else if (b.Harmful)
                    {
                        var actOdds = new Chance(10, 90).Add("이미 보인 조짐", odds.Percent);
                        if (actOdds.Roll(g.Rng)) result.Add(MakeAct(p, b, odds));
                    }
                    else if (odds.Roll(g.Rng)) result.Add(MakeGood(p, b, odds));
                }
            return result;
        }

        static GameEvent MakeSign(Person p, NpcBehavior b, Chance odds)
        {
            return new GameEvent
            {
                Id = $"sign:{b.Id}:{p.Id}", CountId = "조짐:" + b.Id, Title = b.SignTitle, SpeakerId = p.Id, Priority = b.Priority - 5, CauseTag = p.Tag, Reason = odds.Explain(),
                OnQueue = g => { p.Signs[b.Id] = g.Day; p.Cooldowns[b.Id] = g.Day + 1; g.Flags.Add($"sign:{b.Id}:{p.Id}"); },
                Body = g => b.SignBody(g, p),
                Options = new[]
                {
                    Opt("직접 불러 이야기를 듣는다", "신뢰가 높으면 멈출 수 있음", g =>
                    {
                        var calm = new Chance(35).Add("신뢰", (p.Trust - 50) / 2).Trait("정직", p.Temper.Honesty, 8).Trait("앙심", p.Temper.Grudge, -6).Trait("명예", p.Temper.Pride, -4);
                        if (calm.Roll(g.Rng))
                        {
                            p.Signs.Remove(b.Id);
                            p.Cooldowns[b.Id] = g.Day + 6;
                            p.ChangeGrievance(-12);
                            p.ChangeTrust(8);
                            g.Write($"{Ko.I(p.Name)} 속내를 털어놓았다. 당분간 괜찮아 보인다.");
                        }
                        else
                        {
                            p.ChangeTrust(-6);
                            if (p.Temper.Pride >= 2) p.Ashamed = true;
                            g.Write($"{Ko.I(p.Name)} 불쾌해하며 입을 닫았다.");
                        }
                    }, $"{p.Name}의 수상한 행동({b.SignTitle})을 듣고 직접 불렀다.", p.Tag),
                    Opt("사흘간 몰래 지켜본다", "실행하면 현장에서 잡음, 들키면 신뢰 하락", g => { p.WatchedUntil = g.Day + 3; if (g.Rng.Next(100) < 20) { p.ChangeTrust(-10); g.Write($"{Ko.I(p.Name)} 감시당하는 것을 눈치챘다."); } }, $"{p.Name}의 수상한 행동({b.SignTitle})을 지켜보기로 했다.", p.Tag),
                    Opt("별일 아닐 것이라 넘긴다", "변화 없음", g => { }, $"{p.Name}의 수상한 행동({b.SignTitle})에 대한 보고를 넘겼다.", p.Tag),
                },
            };
        }

        static GameEvent MakeAct(Person p, NpcBehavior b, Chance odds)
        {
            string body = "";
            return new GameEvent
            {
                Id = $"act:{b.Id}:{p.Id}", CountId = "실행:" + b.Id, Title = b.ActTitle, SpeakerId = p.Id, Priority = b.Priority, Major = b.Major, CauseTag = p.Tag, Reason = odds.Explain(),
                Foreshadows = new[] { $"sign:{b.Id}:{p.Id}" },
                OnQueue = g =>
                {
                    p.Signs.Remove(b.Id);
                    p.Cooldowns[b.Id] = g.Day + 5;
                    if (p.WatchedUntil >= g.Day)
                    {
                        g.Record($"{g.Day}일차: {p.Name}의 {b.Name} 시도를 현장에서 막았다. 이유 {odds.Explain()}", p.Tag);
                        g.Flags.Add("caught:" + p.Id);
                        p.Memory.Add("caught");
                        body = $"지켜보던 사람들이 현장에서 {Ko.Eul(p.Name)} 붙잡았어요. {Ko.Eul(b.Name)} 시도하던 중이었어요. 피해는 없었어요.";
                        return;
                    }
                    body = b.Act(g, p);
                    g.Record($"{g.Day}일차: {body} 이유 {odds.Explain()}", p.Tag);
                    p.Diary.Add($"{g.Day}일차: {b.Name}");
                },
                Body = g => body,
                Options = ResponseOptions(p, b),
            };
        }

        static EventOption[] ResponseOptions(Person p, NpcBehavior b)
        {
            return new[]
            {
                Opt("모두 앞에서 처벌한다", "질서 회복, 대상의 원한과 수치 증가", g =>
                {
                    if (!p.Present) { g.ChangeMorale(1); return; }
                    p.ChangeTrust(-25);
                    p.ChangeGrievance(20);
                    if (p.Temper.Pride >= 1) p.Ashamed = true;
                    p.Diary.Add($"{g.Day}일차: 모두 앞에서 처벌받았다.");
                    g.ChangeAnger(-3);
                }, $"{p.Name}의 {Ko.Eul(b.Name)} 공개 처벌했다.", p.Tag),
                Opt("조용히 타이르고 넘어간다", "대상의 신뢰 상승, 분노 +3", g =>
                {
                    if (!p.Present) { g.ChangeMorale(-1); return; }
                    p.ChangeTrust(8);
                    p.ChangeGrievance(-10);
                    p.Diary.Add($"{g.Day}일차: 잘못을 용서받았다.");
                    g.ChangeAnger(3);
                }, $"{p.Name}의 {Ko.Eul(b.Name)} 조용히 넘어갔다.", p.Tag),
                Opt("기지에서 내쫓는다", "사기 -4, 앙심이 크면 위치가 샐 수 있음", g =>
                {
                    if (!p.Present) { g.ChangeMorale(-1); return; }
                    p.Health = Health.Gone;
                    g.ChangeMorale(-4);
                    var leak = new Chance(10).Trait("앙심", p.Temper.Grudge, 12).Trait("충성", p.Temper.Loyalty, -5);
                    if (leak.Roll(g.Rng)) { g.ChangeDetection(12); g.Write($"쫓겨난 {Ko.I(p.Name)} 기지 위치를 흘리고 다니는 듯하다."); }
                }, $"{b.Name} 때문에 {Ko.Eul(p.Name)} 내쫓았다.", p.Tag),
            };
        }

        static GameEvent MakeGood(Person p, NpcBehavior b, Chance odds)
        {
            string body = "";
            return new GameEvent
            {
                Id = $"good:{b.Id}:{p.Id}", CountId = "선행:" + b.Id, Title = b.ActTitle, SpeakerId = p.Id, Priority = b.Priority, CauseTag = p.Tag, Reason = odds.Explain(),
                OnQueue = g =>
                {
                    p.Cooldowns[b.Id] = g.Day + 5;
                    body = b.Act(g, p);
                    g.Record($"{g.Day}일차: {body}", p.Tag);
                    p.Diary.Add($"{g.Day}일차: {b.Name}");
                },
                Body = g => body,
                Options = new[]
                {
                    Opt("모두 앞에서 칭찬한다", "대상의 신뢰 +8, 명예심이 높으면 더 기뻐함", g => { p.ChangeTrust(8 + Math.Max(0, p.Temper.Pride) * 3); p.ChangeGrievance(-4); g.ChangeMorale(1); }, null, null),
                    Opt("식량을 조금 더 챙겨준다", "식량 -1, 대상의 불만 -10", g => { g.Add(Res.Food, -1); p.ChangeGrievance(-10); p.ChangeTrust(4); }, null, null, g => g.Stock[Res.Food] >= 1),
                    Opt("당연한 일이니 넘어간다", "대상의 신뢰 -2", g => p.ChangeTrust(-2), null, null),
                },
            };
        }

        static EventOption Opt(string text, string hint, Action<Game> apply, string chronicle, string tag, Func<Game, bool> available = null)
        {
            return new EventOption { Text = text, Hint = hint, Apply = apply, Chronicle = chronicle, Tag = tag, Available = available };
        }
    }
}
