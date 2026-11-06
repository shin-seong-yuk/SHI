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

        public static List<GameEvent> Create()
        {
            return new List<GameEvent>
            {
                new GameEvent
                {
                    Id = "food_shortage", Title = "바닥난 구역", SpeakerId = "hanna", Priority = 50, CauseTag = "yun",
                    Condition = g => g.Day >= 3,
                    Body = g => "근처 상가 지하의 통조림이 바닥났어요. 지금처럼 찾아서는 모두가 먹을 만큼 구하기 어려워요. 배급을 줄이거나, 위험을 안고 더 멀리 나가야 해요.",
                    Options = new[]
                    {
                        Opt("배급을 절반으로 줄인다", "식량 소모 감소, 매일 사기 하락과 분노 증가", g => { g.Ration = Ration.Reduced; g.FoodYieldPenalty = 1; g.Flags.Add("ration_cut"); }, "식량이 부족해 배급을 절반으로 줄였다.", "yun"),
                        Opt("배급을 유지하고 더 멀리 나간다", "탐지도 +8, 배급은 유지", g => { g.FoodYieldPenalty = 0; g.ChangeDetection(8); g.Flags.Add("search_far"); }, "배급을 유지하려고 수색 범위를 넓혔다.", "search"),
                    }
                },
                new GameEvent
                {
                    Id = "recruit_oh", Title = "길가의 노인", SpeakerId = "seojin", Priority = 40,
                    Condition = g => g.Day >= 4,
                    Body = g => "방벽 앞에 다친 노인이 쓰러져 있어요. 다리를 절어서 당장 일은 못 할 거예요. 해진 작업복에 전선 그을음이 묻어 있는데, 이름도 제대로 못 대요.",
                    Options = new[]
                    {
                        Opt("기지에 받아들인다", "식량 부담 증가, 일은 할 수 없음", g => { g.AddPerson("oh", "오 씨", "떠돌이 노인", "다리를 전다. 낡은 작업복을 입고 있다.", Role.Rest, 0, true); g.ChangeMorale(4); g.Flags.Add("oh_joined"); }, "일할 수 없는 노인 오 씨를 받아들였다.", "oh"),
                        Opt("식량만 나눠주고 보낸다", "식량 -2", g => { g.Add(Res.Food, -2); }, "오 씨에게 식량만 주고 돌려보냈다.", "oh"),
                        Opt("모른 척한다", "사기 -4", g => { g.ChangeMorale(-4); g.Flags.Add("oh_left"); }, "쓰러진 노인을 모른 척했다.", "oh"),
                    }
                },
                new GameEvent
                {
                    Id = "minjae_tired", Title = "떨리는 손", SpeakerId = "minjae", Priority = 45, CauseTag = "overtime",
                    Condition = g => g.OvertimeCount >= 1 && g.IsPresent("minjae"),
                    Body = g => "괜찮아요. 그냥 어제 잠을 못 자서 손이 좀 떨리는 것뿐이에요. 배터리 회수 일은 계속할 수 있어요.",
                    Options = new[]
                    {
                        Opt("민재를 하루 쉬게 한다", "오늘 민재의 작업 중단", g => { var m = g.Find("minjae"); m.Role = Role.Rest; g.Flags.Add("minjae_rested"); }, "손을 떠는 민재를 쉬게 했다.", "overtime"),
                        Opt("계속 일하게 둔다", "생산 유지", g => { g.Flags.Add("minjae_warned"); }, "손을 떠는 민재에게 일을 계속 맡겼다.", "overtime"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_medicine", Title = "윤 씨의 부탁", SpeakerId = "yun", Priority = 90, CauseTag = "yun",
                    Condition = g => g.Day >= 7 && g.IsPresent("yun"),
                    Body = g => "제 딸의 열이 사흘째 내리지 않습니다. 장부를 맡은 제가 이런 말씀 드리면 안 되는 건 압니다. 약품을 조금만 내주실 수 없겠습니까.",
                    Options = new[]
                    {
                        Opt("약을 충분히 내준다", "약품 -2", g => { g.Add(Res.Medicine, -2); g.Remember("yun", "helped"); g.Flags.Add("yun_helped"); }, "윤 씨의 딸에게 약을 충분히 내주었다.", "yun", g => g.Stock[Res.Medicine] >= 2),
                        Opt("한 알만 내준다", "약품 -1, 충분하지는 않음", g => { g.Add(Res.Medicine, -1); g.Remember("yun", "half"); g.Flags.Add("yun_half"); }, "윤 씨의 딸에게 약을 한 알만 내주었다.", "yun", g => g.Stock[Res.Medicine] >= 1),
                        Opt("부상자 몫이라며 거절한다", "약품 유지", g => { g.Remember("yun", "refused"); g.Flags.Add("yun_refused"); }, "부상자 몫이라며 윤 씨의 부탁을 거절했다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_ledger", Title = "맞지 않는 장부", SpeakerId = "hanna", Priority = 85, CauseTag = "yun",
                    Foreshadows = new[] { "yun_ledger_seen" },
                    Condition = g => g.Day >= 9 && g.IsPresent("yun") && (g.Flags.Contains("yun_refused") || g.Flags.Contains("yun_half")),
                    Body = g => "윤 씨가 요즘 밤마다 장부를 고치고 있어요. 약품 수량이 어제와 달라요. 창고 근처에서 낯선 드론 소리를 들었다는 사람도 있고요.",
                    Options = new[]
                    {
                        Opt("윤 씨를 불러 직접 묻는다", "진실을 알 수 있지만 사이가 틀어질 수 있음", g => { g.Flags.Add("yun_ledger_seen"); g.Flags.Add("yun_questioned"); }, "장부가 이상하다는 말을 듣고 윤 씨를 불렀다.", "yun"),
                        Opt("착각일 것이라 넘긴다", "분노 변화 없음", g => { g.Flags.Add("yun_ledger_seen"); }, "장부가 이상하다는 보고를 넘겼다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_confess", Title = "쪽지", SpeakerId = "yun", Priority = 95, CauseTag = "yun",
                    Condition = g => g.Flags.Contains("yun_questioned") && g.IsPresent("yun"),
                    Body = g => "PHS가 쪽지를 보냈습니다. 기지 위치를 넘기면 제 딸을 치료해준다고요. 아직 답하지 않았습니다. 하지만 약이 없으면 저는, 저는 무엇을 할지 모르겠습니다.",
                    Options = new[]
                    {
                        Opt("남은 약을 내주고 쪽지를 태운다", "약품 -2, 윤 씨의 배신을 막음", g => { g.Add(Res.Medicine, -2); g.Flags.Add("yun_resolved"); g.Remember("yun", "forgiven"); g.ChangeAnger(4); }, "PHS의 제안을 고백한 윤 씨에게 약을 내주었다.", "yun", g => g.Stock[Res.Medicine] >= 2),
                        Opt("창고 열쇠를 빼앗고 감시한다", "분노 +6, 윤 씨는 일을 못함", g => { g.Flags.Add("yun_resolved"); g.Flags.Add("yun_watched"); g.Find("yun").Role = Role.Rest; g.ChangeAnger(6); }, "고백한 윤 씨에게서 열쇠를 빼앗고 감시했다.", "yun"),
                        Opt("딸과 함께 기지에서 내보낸다", "윤 씨 이탈, 사기 -8", g => { g.Find("yun").Health = Health.Gone; g.Flags.Add("yun_resolved"); g.ChangeMorale(-8); }, "윤 씨와 딸을 기지에서 내보냈다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "yun_betrayal", Title = "털린 약품 창고", SpeakerId = "hanna", Major = true, Priority = 100, CauseTag = "yun",
                    Foreshadows = new[] { "yun_ledger_seen" },
                    Condition = g => g.Day >= 12 && g.IsPresent("yun") && g.Flags.Contains("yun_ledger_seen") && !g.Flags.Contains("yun_resolved") && (g.Flags.Contains("yun_refused") || g.Anger >= 40),
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
                    Condition = g => g.Day >= 11 && g.IsPresent("yun") && g.Flags.Contains("yun_helped"),
                    Body = g => "딸의 열이 내렸습니다. 예전에 창고 일을 하며 봐둔 전쟁 전 비축고가 하나 있습니다. 혹시 몰라 장부에 올리지 않았던 곳입니다. 이제 말씀드립니다.",
                    Options = new[]
                    {
                        Opt("고맙게 받는다", "식량 +8, 사기 +5", g => { g.Add(Res.Food, 8); g.ChangeMorale(5); }, "윤 씨가 약을 받은 보답으로 비축고를 알려주었다.", "yun"),
                        Opt("왜 숨겼는지 추궁한다", "식량 +8, 분노 +4", g => { g.Add(Res.Food, 8); g.ChangeAnger(4); g.Remember("yun", "scolded"); }, "비축고를 숨겼던 윤 씨를 추궁했다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "family_blame", Title = "왜 그 아이만", SpeakerId = "dohyun", Major = true, Priority = 92, CauseTag = "yun",
                    Foreshadows = new[] { "yun_helped" },
                    Condition = g => g.Flags.Contains("yun_helped") && g.Stock[Res.Medicine] == 0 && g.People.Any(p => p.Health == Health.Injured),
                    Body = g => $"{Ko.I(g.People.First(p => p.Health == Health.Injured).Name)} 약이 없어 앓고 있습니다. 며칠 전 윤 씨 딸에게 내준 약이 있었다면 달랐겠지요. 왜 그 아이만 챙겼는지 모두 앞에서 답해주십시오.",
                    Options = new[]
                    {
                        Opt("내 결정이었다고 인정한다", "분노 +5, 사기 +2", g => { g.ChangeAnger(5); g.ChangeMorale(2); }, "윤 씨에게 약을 내준 결정의 책임을 인정했다.", "yun"),
                        Opt("먼 병원 터에 약을 찾으러 보낸다", "탐지도 +10, 약품 +2", g => { g.ChangeDetection(10); g.Add(Res.Medicine, 2); }, "약을 구하려고 위험한 병원 터로 사람을 보냈다.", "yun"),
                    }
                },
                new GameEvent
                {
                    Id = "minjae_accident", Title = "사고", SpeakerId = "hanna", Major = true, Priority = 88, CauseTag = "overtime",
                    Foreshadows = new[] { "minjae_warned" },
                    Condition = g => g.OvertimeCount >= 2 && g.Flags.Contains("minjae_warned") && !g.Flags.Contains("minjae_rested") && g.IsPresent("minjae"),
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
                    Condition = g => g.Day >= 8 && g.Flags.Contains("oh_joined") && g.IsPresent("oh"),
                    Body = g => "밥값은 해야지. 전쟁 전에 변전소에서 일했네. 저 차폐기, 회로 하나만 고치면 배터리를 훨씬 덜 먹을 걸세.",
                    Options = new[]
                    {
                        Opt("회로를 맡긴다", "고철 -3, 차폐기 배터리 소모 -1", g => { g.Add(Res.Scrap, -3); g.Flags.Add("oh_tuned"); g.ChangeMorale(3); }, "오 씨에게 차폐기 회로를 맡겼다.", "oh", g => g.Stock[Res.Scrap] >= 3),
                        Opt("정체를 알 수 없으니 믿지 않는다", "변화 없음", g => { g.Remember("oh", "distrusted"); }, "오 씨의 수리 제안을 믿지 않았다.", "oh"),
                    }
                },
                new GameEvent
                {
                    Id = "dohyun_meeting", Title = "도현의 요구", SpeakerId = "dohyun", Priority = 65,
                    Condition = g => g.Anger >= 50 && g.IsPresent("dohyun"),
                    Body = g => "사람들이 불만을 삼키고 있습니다. 배급과 일을 혼자 정하지 말고 회의를 열어 주십시오. 저는 군대에서 이렇게 무너지는 부대를 본 적이 있습니다.",
                    Options = new[]
                    {
                        Opt("회의를 열어 배급을 되돌린다", "분노 -15, 배급 정상화", g => { g.ChangeAnger(-15); g.Ration = Ration.Normal; }, "도현의 요구로 회의를 열고 배급을 되돌렸다.", "anger"),
                        Opt("결정은 내가 한다고 못 박는다", "분노 +8, 도현의 불신", g => { g.ChangeAnger(8); g.Remember("dohyun", "dismissed"); }, "회의를 요구한 도현을 물리쳤다.", "anger"),
                    }
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
