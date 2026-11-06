using System;
using System.Collections.Generic;
using System.Linq;

namespace SHI.Core
{
    // Personality axes run from -3 to +3. Behaviors turn them into probability changes.
    public sealed class Temperament
    {
        public string Id, Name, Description, FirstImpression;
        public int Self, Group, Pride, Greed, Impulse, Courage, Empathy, Grudge, Honesty, Loyalty, Mood;
    }

    public static class Temperaments
    {
        static Temperament T(string id, string name, string description, string first, int self = 0, int group = 0, int pride = 0, int greed = 0, int impulse = 0, int courage = 0, int empathy = 0, int grudge = 0, int honesty = 0, int loyalty = 0, int mood = 0)
        {
            return new Temperament { Id = id, Name = name, Description = description, FirstImpression = first, Self = self, Group = group, Pride = pride, Greed = greed, Impulse = impulse, Courage = courage, Empathy = empathy, Grudge = grudge, Honesty = honesty, Loyalty = loyalty, Mood = mood };
        }

        public static readonly List<Temperament> All = new List<Temperament>
        {
            T("survivor", "각자도생형", "자신의 안위가 언제나 먼저다. 위험해지면 가장 먼저 등을 돌린다.", "말끝마다 자기 몫을 확인한다.", self: 3, group: -2, loyalty: -1, courage: -1),
            T("devoted", "헌신형", "자신의 안위보다 단체의 이익을 중요하게 여긴다.", "자기 짐보다 남의 짐을 먼저 든다.", self: -2, group: 3, courage: 1, empathy: 1, loyalty: 1),
            T("saving_face", "체면형", "평소에는 단체를 챙기지만, 명예가 실추될 것 같으면 이기적으로 변한다.", "옷매무새와 평판에 유난히 신경 쓴다.", group: 2, pride: 3, grudge: 1),
            T("profiteer", "악덕 거래꾼", "자신의 돈을 위해서라면 악한 일도 할 수 있다.", "모든 물건의 값을 묻는다.", self: 1, greed: 3, empathy: -2, honesty: -2),
            T("weak_will", "의지박약형", "무엇을 해야 하는지 알지만 욕망을 참기가 너무 힘들다.", "약속은 잘하는데 눈빛이 자꾸 흔들린다.", impulse: 3, empathy: 1, honesty: 1),
            T("family_first", "가족우선형", "가족을 위해서라면 무엇이든 할 수 있다.", "낡은 가족사진을 품에 넣고 다닌다.", self: 1, empathy: 2, loyalty: -1, honesty: 1),
            T("principled", "원칙주의자", "규칙을 어기는 일을 참지 못한다. 지도자도 예외는 아니다.", "약속한 시간과 수량을 꼭 되묻는다.", group: 1, courage: 1, grudge: 1, honesty: 3),
            T("blind_loyal", "맹목적 충성파", "한번 따르기로 한 사람은 끝까지 따른다.", "지시를 받으면 이유를 묻지 않는다.", group: 1, courage: 1, honesty: 1, loyalty: 3),
            T("opportunist", "기회주의자", "유리한 쪽으로 언제든 편을 바꾼다.", "누가 실세인지부터 살핀다.", self: 2, greed: 1, honesty: -1, loyalty: -3),
            T("avenger", "복수자", "받은 상처를 절대 잊지 않고 반드시 되갚는다.", "오래된 이름 하나를 자주 중얼거린다.", courage: 2, empathy: -1, grudge: 3),
            T("coward", "겁쟁이", "위험 앞에서 몸이 먼저 도망친다.", "작은 소리에도 어깨가 움츠러든다.", self: 2, courage: -3, empathy: 1),
            T("daredevil", "무모한 용사", "두려움을 모르고 앞뒤를 재지 않는다.", "상처 자랑을 하며 웃는다.", impulse: 2, courage: 3, self: -1),
            T("cold_calc", "냉혈 계산가", "감정 없이 손익만 따진다.", "사람을 볼 때 머릿수를 세는 것 같다.", self: 1, empathy: -3, impulse: -2, honesty: -1),
            T("efficiency", "효율 신봉자", "PHS의 방식이 옳았을지도 모른다고 믿는다.", "낭비라는 말을 자주 쓴다.", group: 1, empathy: -2, loyalty: -2),
            T("saint", "성자", "자기 몫을 나눠주는 데 망설임이 없다.", "제 밥을 덜어 옆 사람에게 건넨다.", self: -3, group: 2, empathy: 3, honesty: 2, mood: 1),
            T("hypocrite", "위선자", "남들 앞에서는 선량하지만 뒤에서는 다르다.", "칭찬받을 때 가장 환하게 웃는다.", self: 1, pride: 2, greed: 1, honesty: -3),
            T("liar", "습관적 거짓말쟁이", "작은 일에도 습관처럼 거짓말을 한다.", "어제 한 이야기가 오늘 조금 다르다.", self: 1, pride: 1, honesty: -3, impulse: 1),
            T("whistleblower", "정직한 고발자", "잘못을 보면 상대가 누구든 말한다.", "눈을 피하지 않고 말한다.", group: 1, courage: 2, honesty: 3),
            T("agitator", "선동가", "불만을 모아 사람들을 움직이는 데 능하다.", "금세 사람들 사이의 중심에 선다.", pride: 2, courage: 1, grudge: 1, loyalty: -3),
            T("recluse", "은둔자", "사람들과 엮이기를 피하고 맡은 일만 한다.", "식사도 멀찍이 떨어져 한다.", self: 1, group: -2, impulse: -1, empathy: -1),
            T("addict", "중독자", "술과 약을 끊지 못한다.", "손끝이 떨리고 술 냄새가 난다.", impulse: 3, self: 1, honesty: -1, mood: -1),
            T("kleptomaniac", "도벽꾼", "필요 없어도 손이 먼저 물건을 집는다.", "주머니가 늘 불룩하다.", greed: 2, impulse: 2, honesty: -2),
            T("paranoid", "피해망상형", "모두가 자신을 해치려 한다고 믿는다.", "등 뒤에 누가 서는 것을 싫어한다.", self: 1, courage: -1, grudge: 2, loyalty: -2, mood: -1),
            T("soldier", "명령 복종형", "명령이 있으면 움직이고, 없으면 멈춘다.", "자세가 곧고 대답이 짧다.", courage: 2, empathy: -1, loyalty: 2, impulse: -2),
            T("caregiver", "돌봄형", "아프고 약한 사람을 그냥 지나치지 못한다.", "다친 사람의 붕대부터 살핀다.", group: 2, empathy: 3, self: -1),
            T("climber", "출세주의자", "더 높은 자리를 위해서라면 동료도 밟는다.", "윗사람의 말은 빠짐없이 기억한다.", pride: 2, greed: 1, group: -1, loyalty: 1, empathy: -1),
            T("optimist", "낙천가", "어떤 상황에서도 웃음을 잃지 않는다.", "폐허에서도 농담을 한다.", group: 1, empathy: 1, mood: 2),
            T("pessimist", "비관론자", "모든 일이 결국 나빠질 것이라 여긴다.", "희망이라는 말에 코웃음 친다.", courage: -1, loyalty: -1, mood: -2),
            T("wounded", "상처 입은 신의형", "쉽게 믿지 않지만, 믿으면 끝까지 간다.", "도움을 받으면 오히려 경계한다.", empathy: 1, grudge: 1, honesty: 2, loyalty: 1),
            T("idealist", "이상주의자", "PHS를 무너뜨리는 대의를 위해서라면 동료의 희생도 받아들인다.", "새로운 세상 이야기를 할 때 눈이 빛난다.", group: 3, courage: 2, empathy: -1, loyalty: -1),
        };

        public static Temperament Get(string id) => All.First(t => t.Id == id);
        public static Temperament PickFrom(Random rng, params string[] ids) => Get(ids[rng.Next(ids.Length)]);
    }

    // A probability made of labeled parts, so the game can explain why something happened.
    public sealed class Chance
    {
        readonly List<KeyValuePair<string, int>> parts = new List<KeyValuePair<string, int>>();
        readonly int max;

        public Chance(int baseChance = 0, int max = 90)
        {
            this.max = max;
            Add("기본", baseChance);
        }

        public Chance Add(string label, int delta)
        {
            if (delta != 0) parts.Add(new KeyValuePair<string, int>(label, delta));
            return this;
        }

        public Chance Trait(string label, int value, int coefficient) => Add("성향·" + label, value * coefficient);

        public int Percent => Math.Max(0, Math.Min(max, parts.Sum(p => p.Value)));
        public bool Roll(Random rng) => rng.Next(100) < Percent;

        public string Explain()
        {
            var shown = parts.Where(p => p.Key != "기본").OrderByDescending(p => Math.Abs(p.Value)).Take(4).Select(p => $"{p.Key} {(p.Value > 0 ? "+" : "")}{p.Value}");
            return $"{Percent}% ({string.Join(", ", shown)})";
        }
    }
}
