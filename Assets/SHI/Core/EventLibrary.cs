using System.Collections.Generic;
using System.Linq;

namespace SHI.Core
{
    public static class EventLibrary
    {
        static EventOption Opt(string text, string hint, System.Action<Game> apply, string chronicle = null, string tag = null, System.Func<Game, bool> available = null)
        {
            return new EventOption { Text = text, Hint = hint, Apply = apply, Chronicle = chronicle, Tag = tag, Available = available };
        }

        static readonly (string Name, string Title)[] Strangers =
        {
            ("강혜린", "전직 교사"), ("박무열", "트럭 운전사"), ("이수아", "약국 직원"), ("최건", "철거 노동자"), ("정다온", "떠돌이 소녀"), ("문태식", "전직 경찰"),
            ("백하람", "통신 기사"), ("오세아", "요리사"), ("임도윤", "대학생"), ("홍석", "폐품 상인"), ("장미래", "간병인"), ("권용", "용접공"),
            ("신바다", "어부"), ("유하진", "기록 담당자"), ("남기태", "탈영병"), ("조은별", "간호조무사"),
        };

        public static List<GameEvent> Create()
        {
            return new List<GameEvent>
            {
                new GameEvent
                {
                    Id = "food_shortage", Title = "바닥난 구역", SpeakerId = "hanna", Priority = 50, CauseTag = "yun",
                    Condition = g => g.Day >= 2,
                    Odds = g => new Chance(5, 95).Add("지난 날들", (g.Day - 1) * 18).Add("수색 인원", g.People.Count(p => p.CanWork && p.Role == Role.Food) * 6),
                    Body = g => "근처 상가 지하의 통조림이 바닥났어요. 지금처럼 찾아서는 모두가 먹을 만큼 구하기 어려워요. 배급을 줄이거나, 위험을 안고 더 멀리 나가야 해요.",
                    Options = new[]
                    {
                        Opt("배급을 절반으로 줄인다", "식량 소모 감소, 매일 사기 하락과 불만 증가", g => { g.Ration = Ration.Reduced; g.FoodYieldPenalty = 1; g.Flags.Add("ration_cut"); }, "식량이 부족해 배급을 절반으로 줄였다.", "yun"),
                        Opt("배급을 유지하고 더 멀리 나간다", "탐지도 +8, 배급은 유지", g => { g.FoodYieldPenalty = 0; g.ChangeDetection(8); g.Flags.Add("search_far"); }, "배급을 유지하려고 수색 범위를 넓혔다.", "search"),
                    }
                },
                new GameEvent
                {
                    Id = "recruit_oh", Title = "길가의 노인", SpeakerId = "seojin", Priority = 40, CauseTag = "oh",
                    Condition = g => g.Day >= 3,
                    Odds = g => new Chance(30).Add("탐사 인원", g.People.Count(p => p.CanWork && (p.Role == Role.Scrap || p.Role == Role.Food)) * 8),
                    Body = g => "방벽 앞에 다친 노인이 쓰러져 있어요. 다리를 절어서 당장 일은 못 할 거예요. 해진 작업복에 전선 그을음이 묻어 있는데, 이름도 제대로 못 대요.",
                    Options = new[]
                    {
                        Opt("기지에 받아들인다", "식량 부담 증가, 일은 할 수 없음", g => { var oh = g.AddPerson("oh", "오 씨", "떠돌이 노인", "다리를 전다. 낡은 작업복을 입고 있다.", Role.Rest, 0, Temperaments.Get("wounded"), true); oh.Trust = 60; g.ChangeMorale(4); g.Flags.Add("oh_joined"); }, "일할 수 없는 노인 오 씨를 받아들였다.", "oh"),
                        Opt("식량만 나눠주고 보낸다", "식량 -2", g => { g.Add(Res.Food, -2); }, "오 씨에게 식량만 주고 돌려보냈다.", "oh"),
                        Opt("모른 척한다", "사기 -4", g => { g.ChangeMorale(-4); g.Flags.Add("oh_left"); }, "쓰러진 노인을 모른 척했다.", "oh"),
                    }
                },
                new GameEvent
                {
                    Id = "minjae_tired", Title = "떨리는 손", SpeakerId = "minjae", Priority = 45, CauseTag = "overtime",
                    Condition = g => g.OvertimeCount >= 1 && g.IsPresent("minjae"),
                    Odds = g => new Chance(30).Add("특근 횟수", g.OvertimeCount * 20),
                    Body = g => "괜찮아요. 그냥 어제 잠을 못 자서 손이 좀 떨리는 것뿐이에요. 배터리 회수 일은 계속할 수 있어요.",
                    Options = new[]
                    {
                        Opt("민재를 하루 쉬게 한다", "오늘 민재의 작업 중단", g => { var m = g.Find("minjae"); m.Role = Role.Rest; m.ChangeTrust(8); g.Flags.Add("minjae_rested"); }, "손을 떠는 민재를 쉬게 했다.", "overtime"),
                        Opt("계속 일하게 둔다", "생산 유지", g => { g.Find("minjae").ChangeGrievance(8); g.Flags.Add("minjae_warned"); }, "손을 떠는 민재에게 일을 계속 맡겼다.", "overtime"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_medicine", Title = "윤 씨의 부탁", SpeakerId = "yun", Priority = 90, CauseTag = "yun",
                    Condition = g => g.Day >= 4 && g.IsPresent("yun"),
                    Odds = g => new Chance(5, 95).Add("딸의 병세", (g.Day - 3) * 15).Add("신뢰", (g.Find("yun").Trust - 50) / 5),
                    Body = g => "제 딸의 열이 사흘째 내리지 않습니다. 장부를 맡은 제가 이런 말씀 드리면 안 되는 건 압니다. 약품을 조금만 내주실 수 없겠습니까.",
                    Options = new[]
                    {
                        Opt("약을 충분히 내준다", "약품 -2", g => { var y = g.Find("yun"); g.Add(Res.Medicine, -2); y.ChangeTrust(25); y.ChangeGrievance(-15); y.Diary.Add($"{g.Day}일차: 딸의 약을 받았다."); g.Remember("yun", "helped"); g.Flags.Add("yun_helped"); }, "윤 씨의 딸에게 약을 충분히 내주었다.", "yun", g => g.Stock[Res.Medicine] >= 2),
                        Opt("한 알만 내준다", "약품 -1, 충분하지는 않음", g => { var y = g.Find("yun"); g.Add(Res.Medicine, -1); y.ChangeGrievance(8); y.Diary.Add($"{g.Day}일차: 약을 한 알만 받았다."); g.Remember("yun", "half"); g.Flags.Add("yun_half"); }, "윤 씨의 딸에게 약을 한 알만 내주었다.", "yun", g => g.Stock[Res.Medicine] >= 1),
                        Opt("부상자 몫이라며 거절한다", "약품 유지, 윤 씨의 신뢰 크게 하락", g => { var y = g.Find("yun"); y.ChangeTrust(-25); y.ChangeGrievance(25); y.Diary.Add($"{g.Day}일차: 딸의 약을 거절당했다."); g.Remember("yun", "refused"); g.Flags.Add("yun_refused"); g.YunRefusedDay = g.Day; }, "부상자 몫이라며 윤 씨의 부탁을 거절했다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_ledger", Title = "맞지 않는 장부", SpeakerId = "hanna", Priority = 85, CauseTag = "yun",
                    Condition = g => g.IsPresent("yun") && (g.Flags.Contains("yun_refused") || g.Flags.Contains("yun_half")),
                    Odds = g => new Chance(15, 95).Add("불만", g.Find("yun").Grievance / 2).Add("신뢰 하락", (50 - g.Find("yun").Trust) / 3),
                    Body = g => "윤 씨가 요즘 밤마다 장부를 고치고 있어요. 약품 수량이 어제와 달라요. 창고 근처에서 낯선 드론 소리를 들었다는 사람도 있고요.",
                    Options = new[]
                    {
                        Opt("윤 씨를 불러 직접 묻는다", "진실을 알 수 있지만 사이가 틀어질 수 있음", g => { g.Flags.Add("yun_ledger_seen"); g.Flags.Add("yun_questioned"); g.YunLedgerDay = g.Day; }, "장부가 이상하다는 말을 듣고 윤 씨를 불렀다.", "yun"),
                        Opt("착각일 것이라 넘긴다", "분노 변화 없음", g => { g.Flags.Add("yun_ledger_seen"); g.YunLedgerDay = g.Day; }, "장부가 이상하다는 보고를 넘겼다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_confess", Title = "쪽지", SpeakerId = "yun", Priority = 95, CauseTag = "yun",
                    Condition = g => g.Flags.Contains("yun_questioned") && g.IsPresent("yun"),
                    Odds = g => new Chance(45, 95).Trait("정직", g.Find("yun").Temper.Honesty, 10).Add("신뢰", g.Find("yun").Trust / 3),
                    Body = g => "PHS가 쪽지를 보냈습니다. 기지 위치를 넘기면 제 딸을 치료해준다고요. 아직 답하지 않았습니다. 하지만 약이 없으면 저는, 저는 무엇을 할지 모르겠습니다.",
                    Options = new[]
                    {
                        Opt("남은 약을 내주고 쪽지를 태운다", "약품 -2, 윤 씨의 배신을 막음", g => { var y = g.Find("yun"); g.Add(Res.Medicine, -2); g.Flags.Add("yun_resolved"); y.ChangeTrust(35); y.ChangeGrievance(-30); g.Remember("yun", "forgiven"); g.ChangeAnger(4); }, "PHS의 제안을 고백한 윤 씨에게 약을 내주었다.", "yun", g => g.Stock[Res.Medicine] >= 2),
                        Opt("창고 열쇠를 빼앗고 감시한다", "분노 +6, 윤 씨는 일을 못함", g => { var y = g.Find("yun"); g.Flags.Add("yun_resolved"); g.Flags.Add("yun_watched"); y.Role = Role.Rest; y.WatchedUntil = g.Day + 30; y.ChangeTrust(-20); y.ChangeGrievance(15); g.ChangeAnger(6); }, "고백한 윤 씨에게서 열쇠를 빼앗고 감시했다.", "yun"),
                        Opt("딸과 함께 기지에서 내보낸다", "윤 씨 이탈, 사기 -8", g => { g.Find("yun").Health = Health.Gone; g.Flags.Add("yun_resolved"); g.ChangeMorale(-8); }, "윤 씨와 딸을 기지에서 내보냈다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_betrayal", Title = "털린 약품 창고", SpeakerId = "hanna", Major = true, Priority = 100, CauseTag = "yun",
                    Foreshadows = new[] { "yun_ledger_seen" },
                    Condition = g => g.IsPresent("yun") && g.Flags.Contains("yun_ledger_seen") && !g.Flags.Contains("yun_resolved") && g.Day >= g.YunLedgerDay + 2,
                    Odds = g => { var y = g.Find("yun"); return new Chance(5).Add("약을 거절당함", g.Flags.Contains("yun_refused") ? 22 : 6).Add("불만", y.Grievance / 3).Add("신뢰 하락", (50 - y.Trust) / 3).Trait("가족우선", y.Temper.Empathy, 3).Trait("정직", y.Temper.Honesty, -3).Add("지켜보는 눈", y.WatchedUntil >= g.Day ? -15 : 0); },
                    Body = g => "약품 창고가 비었어요. 윤 씨와 딸도 사라졌고요. 윤 씨 숙소에서 쪽지가 나왔어요. 기지 위치를 넘기면 딸을 치료해주겠다는 PHS의 제안이에요.",
                    Options = new[]
                    {
                        Opt("습격에 대비하라고 명령한다", "약품 전부 손실, 탐지도 +35, 오늘 밤 습격, 사기 -6", g => { Betray(g); g.ChangeMorale(-6); }, "윤 씨가 약품을 들고 사라졌고, PHS가 기지 위치를 알게 됐다.", "yun"),
                        Opt("모두를 모아 사실을 그대로 말한다", "약품 전부 손실, 탐지도 +35, 오늘 밤 습격, 분노 +4", g => { Betray(g); g.ChangeAnger(4); }, "윤 씨의 배신을 사람들에게 숨기지 않았다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_reward", Title = "장부 밖의 식량", SpeakerId = "yun", Priority = 60, CauseTag = "yun",
                    Condition = g => g.IsPresent("yun") && g.Flags.Contains("yun_helped"),
                    Odds = g => new Chance(15, 95).Add("신뢰", (g.Find("yun").Trust - 50) / 2).Add("지난 날들", (g.Day - 7) * 5),
                    Body = g => "딸의 열이 내렸습니다. 예전에 창고 일을 하며 봐둔 전쟁 전 비축고가 하나 있습니다. 혹시 몰라 장부에 올리지 않았던 곳입니다. 이제 말씀드립니다.",
                    Options = new[]
                    {
                        Opt("고맙게 받는다", "식량 +8, 사기 +5", g => { g.Add(Res.Food, 8); g.ChangeMorale(5); g.Find("yun").ChangeTrust(5); }, "윤 씨가 약을 받은 보답으로 비축고를 알려주었다.", "yun"),
                        Opt("왜 숨겼는지 추궁한다", "식량 +8, 분노 +4", g => { var y = g.Find("yun"); g.Add(Res.Food, 8); g.ChangeAnger(4); y.ChangeTrust(-10); y.Ashamed = true; g.Remember("yun", "scolded"); }, "비축고를 숨겼던 윤 씨를 추궁했다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "family_blame", Title = "왜 그 아이만", SpeakerId = "dohyun", Major = true, Priority = 92, CauseTag = "yun",
                    Foreshadows = new[] { "yun_helped" },
                    Condition = g => g.Flags.Contains("yun_helped") && g.IsPresent("dohyun") && g.Stock[Res.Medicine] == 0 && g.People.Any(p => p.Health == Health.Injured),
                    Odds = g => new Chance(30, 95).Trait("앙심", g.Find("dohyun").Temper.Grudge, 8).Trait("정직", g.Find("dohyun").Temper.Honesty, 4).Trait("충성", g.Find("dohyun").Temper.Loyalty, -6),
                    Body = g => $"{Ko.I(g.People.FirstOrDefault(p => p.Health == Health.Injured)?.Name ?? "다친 사람")} 약이 없어 앓고 있습니다. 며칠 전 윤 씨 딸에게 내준 약이 있었다면 달랐겠지요. 왜 그 아이만 챙겼는지 모두 앞에서 답해주십시오.",
                    Options = new[]
                    {
                        Opt("내 결정이었다고 인정한다", "분노 +5, 사기 +2", g => { g.ChangeAnger(5); g.ChangeMorale(2); g.Find("dohyun").ChangeTrust(5); }, "윤 씨에게 약을 내준 결정의 책임을 인정했다.", "yun"),
                        Opt("먼 병원 터에 약을 찾으러 보낸다", "탐지도 +10, 약품 +2", g => { g.ChangeDetection(10); g.Add(Res.Medicine, 2); }, "약을 구하려고 위험한 병원 터로 사람을 보냈다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "minjae_accident", Title = "사고", SpeakerId = "hanna", Major = true, Priority = 88, CauseTag = "overtime",
                    Foreshadows = new[] { "minjae_warned" },
                    Condition = g => g.OvertimeCount >= 2 && g.Flags.Contains("minjae_warned") && !g.Flags.Contains("minjae_rested") && g.IsPresent("minjae"),
                    Odds = g => new Chance(15).Add("특근 횟수", g.OvertimeCount * 15),
                    Body = g => "민재가 배터리를 옮기다 쓰러졌어요. 손이 떨린다고 말했었잖아요. 다음에는 누가 쓰러질까요.",
                    Options = new[]
                    {
                        Opt("당분간 특근을 없앤다고 약속한다", "분노 -6, 사기 +2", g => { g.InjurePerson(g.Find("minjae")); g.ChangeAnger(-6); g.ChangeMorale(2); g.Flags.Add("no_overtime_promise"); }, "민재의 사고 뒤에 특근을 멈추겠다고 약속했다.", "overtime"),
                        Opt("사고는 일어나는 법이라며 넘어간다", "분노 +10", g => { g.InjurePerson(g.Find("minjae")); g.ChangeAnger(10); }, "민재의 사고를 대수롭지 않게 넘겼다.", "overtime"),
                    }
                },
                new GameEvent
                {
                    Id = "oh_reveal", Title = "낡은 작업복", SpeakerId = "oh", Priority = 70, CauseTag = "oh",
                    Foreshadows = new[] { "oh_joined" },
                    Condition = g => g.Flags.Contains("oh_joined") && g.IsPresent("oh"),
                    Odds = g => new Chance(15).Add("신뢰", (g.Find("oh").Trust - 40) / 2).Add("차폐기 노후", g.Stock[Res.Battery] < 4 ? 15 : 0),
                    Body = g => "밥값은 해야지. 전쟁 전에 변전소에서 일했네. 저 차폐기, 회로 하나만 고치면 배터리를 훨씬 덜 먹을 걸세.",
                    Options = new[]
                    {
                        Opt("회로를 맡긴다", "고철 -3, 차폐기 배터리 소모 -1", g => { g.Add(Res.Scrap, -3); g.Flags.Add("oh_tuned"); g.ChangeMorale(3); g.Find("oh").ChangeTrust(10); }, "오 씨에게 차폐기 회로를 맡겼다.", "oh", g => g.Stock[Res.Scrap] >= 3),
                        Opt("정체를 알 수 없으니 믿지 않는다", "오 씨의 신뢰 -15", g => { g.Remember("oh", "distrusted"); g.Find("oh").ChangeTrust(-15); }, "오 씨의 수리 제안을 믿지 않았다.", "oh"),
                    }
                },
                new GameEvent
                {
                    Id = "dohyun_meeting", Title = "도현의 요구", SpeakerId = "dohyun", Priority = 65, CauseTag = "anger",
                    Condition = g => g.Anger >= 35 && g.IsPresent("dohyun"),
                    Odds = g => { var d = g.Find("dohyun"); return new Chance(10, 95).Add("분노", g.Anger - 35).Trait("정직", d.Temper.Honesty, 6).Trait("충성", d.Temper.Loyalty, -5); },
                    Body = g => "사람들이 불만을 삼키고 있습니다. 배급과 일을 혼자 정하지 말고 회의를 열어 주십시오. 저는 군대에서 이렇게 무너지는 부대를 본 적이 있습니다.",
                    Options = new[]
                    {
                        Opt("회의를 열어 배급을 되돌린다", "분노 -15, 배급 정상화", g => { g.ChangeAnger(-15); g.Ration = Ration.Normal; g.Find("dohyun").ChangeTrust(12); }, "도현의 요구로 회의를 열고 배급을 되돌렸다.", "anger"),
                        Opt("결정은 내가 한다고 못 박는다", "분노 +8, 도현의 불신", g => { var d = g.Find("dohyun"); g.ChangeAnger(8); d.ChangeTrust(-15); d.ChangeGrievance(15); if (d.Temper.Pride >= 1) d.Ashamed = true; g.Remember("dohyun", "dismissed"); }, "회의를 요구한 도현을 물리쳤다.", "anger"),
                    }
                },
            };
        }

        public static bool HasStrangers(Game g) => Strangers.Any(s => !g.People.Any(p => p.Name == s.Name));

        // A stranger at the gate. Their true temperament is revealed only after a few days together.
        public static GameEvent MakeStranger(Game g)
        {
            var unused = Strangers.Where(s => !g.People.Any(p => p.Name == s.Name)).ToList();
            var pick = unused[g.Rng.Next(unused.Count)];
            var temper = Temperaments.All[g.Rng.Next(Temperaments.All.Count)];
            bool frail = g.Rng.Next(100) < 18;
            int skill = g.Rng.Next(0, 3);
            return new GameEvent
            {
                Id = "stranger:" + pick.Name, CountId = "영입", Title = "문 앞의 사람", SpeakerId = "stranger", Priority = 35, CauseTag = "recruit",
                Body = _ => $"저는 {pick.Name}, 직업은 {pick.Title}입니다. 먹을 것만 주시면 무슨 일이든 하겠습니다.\n첫인상: {temper.FirstImpression}" + (frail ? "\n몸이 많이 상해 당장은 일할 수 없어 보인다." : ""),
                Options = new[]
                {
                    Opt("받아들인다", frail ? "식량 부담 증가, 일은 못함, 성향은 함께 지내봐야 앎" : "식량 부담 증가, 성향은 함께 지내봐야 앎", x =>
                    {
                        var p = x.AddPerson("r" + x.People.Count, pick.Name, pick.Title, "길에서 받아들인 사람이다.", Role.Rest, skill, temper, frail);
                        p.TemperKnown = false;
                        p.JoinedDay = x.Day;
                        p.Trust = 55;
                        x.ChangeMorale(2);
                        x.Accepted++;
                    }, $"{Ko.Eul(pick.Name)} 받아들였다.", "recruit"),
                    Opt("식량만 주고 보낸다", "식량 -2, 사기 +1", x => { x.Add(Res.Food, -2); x.ChangeMorale(1); }, $"{pick.Name}에게 식량만 주고 돌려보냈다.", "recruit", x => x.Stock[Res.Food] >= 2),
                    Opt("문을 닫는다", "사기 -2, 소문이 나면 영입 가능성 감소", x => { x.ChangeMorale(-2); x.Refused++; }, $"{Ko.Eul(pick.Name)} 돌려보냈다.", "recruit"),
                },
            };
        }

        static void Betray(Game g)
        {
            g.Stock[Res.Medicine] = 0;
            g.Find("yun").Health = Health.Gone;
            g.ChangeDetection(35);
            g.ForcedRaid = true;
            g.RaidTonight = true;
            g.DroneSeen = true;
            g.Flags.Add("yun_betrayed");
        }
    }
}
