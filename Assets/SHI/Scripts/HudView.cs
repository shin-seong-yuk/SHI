using System;
using System.Collections.Generic;
using System.Linq;
using SHI.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SHI.Client
{
    public sealed class HudView : MonoBehaviour
    {
        static readonly Color Panel = new Color(0.035f, 0.05f, 0.08f, 0.9f);
        static readonly Color Row = new Color(0.1f, 0.13f, 0.18f, 1f);
        static readonly Color ButtonColor = new Color(0.13f, 0.18f, 0.25f, 1f);
        static readonly Color Selected = new Color(0.08f, 0.42f, 0.55f, 1f);
        static readonly Color Disabled = new Color(0.1f, 0.11f, 0.13f, 1f);
        static readonly Color Ink = new Color(0.93f, 0.95f, 0.98f);
        static readonly Color Quiet = new Color(0.62f, 0.69f, 0.77f);

        public static readonly Dictionary<Role, string> RoleNames = new Dictionary<Role, string>
        {
            { Role.Rest, "휴식" }, { Role.Store, "창고 관리" }, { Role.Scrap, "고철 수집" },
            { Role.Food, "식량 수색" }, { Role.Battery, "배터리 회수" }, { Role.Guard, "경비" },
        };

        Transform canvas;
        Transform topBar, leftPanel, rightPanel, bottomPanel, modalLayer;
        readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        GameController controller;

        public void Init(GameController owner)
        {
            controller = owner;
            var go = new GameObject("HUD");
            go.transform.SetParent(transform, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            canvas = go.transform;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            topBar = MakePanel("Top", 0, 1, 1, 1, new Vector2(0, -44), new Vector2(0, 88)).transform;
            leftPanel = MakePanel("People", 0, 0.5f, 0, 0.5f, new Vector2(186, -44), new Vector2(356, 720)).transform;
            rightPanel = MakePanel("Actions", 1, 0.5f, 1, 0.5f, new Vector2(-186, -44), new Vector2(356, 720)).transform;
            bottomPanel = MakePanel("Bottom", 0.5f, 0, 0.5f, 0, new Vector2(0, 94), new Vector2(1160, 172)).transform;
            modalLayer = new GameObject("Modal", typeof(RectTransform)).transform;
            modalLayer.SetParent(canvas, false);
            Stretch((RectTransform)modalLayer);
        }

        public bool PointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void Refresh(Core.Game game, BuildingType? placing)
        {
            Clear(topBar); Clear(leftPanel); Clear(rightPanel); Clear(bottomPanel);
            DrawTop(game);
            DrawPeople(game);
            DrawActions(game, placing);
            DrawBottom(game, placing);
        }

        public void SetPlayVisible(bool visible)
        {
            leftPanel.gameObject.SetActive(visible);
            rightPanel.gameObject.SetActive(visible);
            bottomPanel.gameObject.SetActive(visible);
        }

        void DrawTop(Core.Game g)
        {
            var phase = g.Phase == Phase.Day ? "낮" : "밤";
            Label(topBar, $"SHI   {Mathf.Min(g.Day, Core.Game.LastDay)}일차 · {phase}", 28, new Vector2(24, -44), new Vector2(290, 60), Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            float x = 330;
            Stat("고철", g.Stock[Res.Scrap].ToString(), ref x, new Color(0.82f, 0.85f, 0.9f));
            Stat("폐배터리", g.Stock[Res.Battery].ToString(), ref x, Art.Cyan);
            Stat($"식량 (-{g.FoodUse}/일)", g.Stock[Res.Food].ToString(), ref x, Art.Amber);
            Stat("약품", g.Stock[Res.Medicine].ToString(), ref x, Art.Pink);
            x += 24;
            Gauge("사기", g.Morale, ref x, Art.Lime);
            Gauge("분노", g.Anger, ref x, new Color(1f, 0.44f, 0.22f));
            Gauge("탐지도", g.Detection, ref x, Art.Red);
            Gauge("PHS 복구", g.Recovery, ref x, new Color(0.73f, 0.5f, 1f));
        }

        void Stat(string name, string value, ref float x, Color color)
        {
            float w = name.Length > 5 ? 136 : 92;
            Label(topBar, name, 16, new Vector2(x, -26), new Vector2(w, 26), Quiet, TextAnchor.MiddleLeft);
            Label(topBar, value, 28, new Vector2(x, -58), new Vector2(w, 36), color, TextAnchor.MiddleLeft, FontStyle.Bold);
            x += w;
        }

        void Gauge(string name, int value, ref float x, Color color)
        {
            const float w = 138;
            Label(topBar, $"{name} {value}", 16, new Vector2(x, -26), new Vector2(w, 26), Quiet, TextAnchor.MiddleLeft);
            var back = Image(topBar, new Vector2(x, -56), new Vector2(w - 18, 14), Row);
            var fill = Image(back.transform, Vector2.zero, new Vector2((w - 18) * value / 100f, 14), color);
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = fill.rectTransform.pivot = new Vector2(0, 0.5f);
            fill.rectTransform.anchoredPosition = Vector2.zero;
            x += w;
        }

        void DrawPeople(Core.Game g)
        {
            Label(leftPanel, "사람들", 22, new Vector2(18, -26), new Vector2(320, 34), Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            float y = -56;
            foreach (var p in g.People.Where(p => p.Health != Health.Gone && p.Health != Health.Dead))
            {
                Image(leftPanel, new Vector2(12, y - 44), new Vector2(332, 88), Row);
                var portrait = new GameObject("Portrait", typeof(RectTransform)).AddComponent<RawImage>();
                portrait.transform.SetParent(leftPanel, false);
                Place(portrait.rectTransform, new Vector2(18, y - 44), new Vector2(76, 76));
                portrait.texture = Portrait(p.Id);
                string state = p.Health == Health.Injured ? " · 부상" : p.Frail ? " · 일 못함" : "";
                Label(leftPanel, $"{p.Name}{state}", 19, new Vector2(102, y - 20), new Vector2(236, 28), p.Health == Health.Injured ? new Color(1f, 0.55f, 0.55f) : Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                Label(leftPanel, p.Title, 15, new Vector2(102, y - 42), new Vector2(120, 22), Quiet, TextAnchor.MiddleLeft);
                if (p.CanWork && g.Phase == Phase.Day)
                {
                    var person = p;
                    Button(leftPanel, RoleNames[p.Role] + "  ▸", 17, new Vector2(102, y - 68), new Vector2(232, 32), ButtonColor, () => controller.CycleRole(person));
                }
                else
                {
                    string role = p.CanWork ? RoleNames[p.Role] : "쉬는 중";
                    Label(leftPanel, role, 17, new Vector2(102, y - 68), new Vector2(232, 32), Quiet, TextAnchor.MiddleLeft);
                }
                y -= 94;
            }
        }

        void DrawActions(Core.Game g, BuildingType? placing)
        {
            Label(rightPanel, "배급", 22, new Vector2(18, -26), new Vector2(320, 34), Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            bool day = g.Phase == Phase.Day;
            RationButton(g, Ration.Reduced, "절반", 14, day);
            RationButton(g, Ration.Normal, "보통", 126, day);
            RationButton(g, Ration.Generous, "넉넉히", 238, day);

            Label(rightPanel, "전파 차폐기", 22, new Vector2(18, -112), new Vector2(320, 34), Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(rightPanel, g.ShieldBoost ? "출력 높임 · 배터리 -" + g.ShieldBatteryUse : "보통 출력 · 배터리 -" + g.ShieldBatteryUse, 17, new Vector2(14, -150), new Vector2(328, 38), g.ShieldBoost ? Selected : ButtonColor, () => controller.ToggleShield());

            Label(rightPanel, g.BuiltToday ? "건설 (오늘은 완료)" : "건설 (하루 1채)", 22, new Vector2(18, -198), new Vector2(320, 34), Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            float y = -234;
            foreach (var type in new[] { BuildingType.Quarters, BuildingType.Clinic, BuildingType.Workshop, BuildingType.Wall, BuildingType.Farm })
            {
                var info = Core.Game.Catalog[type];
                string cost = $"고철 {info.ScrapCost}" + (info.BatteryCost > 0 ? $" · 배터리 {info.BatteryCost}" : "");
                bool can = day && !g.BuiltToday && g.CanAfford(type);
                var buildingType = type;
                Button(rightPanel, $"{info.Name}   {cost}\n<size=14>{info.Description}</size>", 17, new Vector2(14, y - 30), new Vector2(328, 60), placing == type ? Selected : can ? ButtonColor : Disabled, can ? () => controller.BeginPlacing(buildingType) : (Action)null);
                y -= 66;
            }
            Label(rightPanel, "빨간 원 건물은 차폐 범위 밖이라 탐지도를 올립니다.", 14, new Vector2(18, y - 16), new Vector2(322, 30), Quiet, TextAnchor.MiddleLeft);
        }

        void RationButton(Core.Game g, Ration ration, string text, float x, bool enabled)
        {
            Button(rightPanel, text, 18, new Vector2(x, -64), new Vector2(104, 40), g.Ration == ration ? Selected : ButtonColor, enabled ? () => controller.SetRation(ration) : (Action)null);
        }

        void DrawBottom(Core.Game g, BuildingType? placing)
        {
            var lines = g.Log.Skip(Math.Max(0, g.Log.Count - 4)).ToList();
            Label(bottomPanel, string.Join("\n", lines), 17, new Vector2(18, -86), new Vector2(620, 156), Quiet, TextAnchor.MiddleLeft);
            if (placing != null)
            {
                Label(bottomPanel, "지도에서 빈 땅을 클릭하세요", 20, new Vector2(660, -58), new Vector2(480, 50), Art.Lime, TextAnchor.MiddleCenter, FontStyle.Bold);
                Button(bottomPanel, "취소", 20, new Vector2(660, -122), new Vector2(480, 56), ButtonColor, () => controller.CancelPlacing());
                return;
            }
            if (g.Phase == Phase.Day)
            {
                if (g.DroneSeen || g.Log.LastOrDefault()?.Contains("드론") == true)
                    Label(bottomPanel, "감시 드론 목격 · 오늘 밤을 대비하세요", 19, new Vector2(660, -36), new Vector2(480, 40), Art.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
                Button(bottomPanel, "밤을 맞이한다", 26, new Vector2(660, -108), new Vector2(480, 84), Selected, () => controller.EndDay());
                return;
            }
            if (g.Phase != Phase.Night) return;
            if (g.RaidTonight)
            {
                Label(bottomPanel, $"습격! 예상 공격력 {g.AttackPower - 2}~{g.AttackPower + 2} · 내 방어력 {g.DefensePower(NightOrder.HoldWalls)}", 18, new Vector2(660, -24), new Vector2(490, 34), Art.Red, TextAnchor.MiddleCenter, FontStyle.Bold);
                Button(bottomPanel, "방벽 사수\n<size=14>위험 낮음</size>", 18, new Vector2(660, -100), new Vector2(156, 96), ButtonColor, () => controller.Raid(NightOrder.HoldWalls));
                Button(bottomPanel, "기습\n<size=14>방어 +2, 부상 위험 큼</size>", 18, new Vector2(822, -100), new Vector2(156, 96), ButtonColor, () => controller.Raid(NightOrder.Ambush));
                bool canBoost = g.Stock[Res.Battery] >= 2;
                Button(bottomPanel, "과부하\n<size=14>방어 +3, 배터리 -2</size>", 18, new Vector2(984, -100), new Vector2(156, 96), canBoost ? ButtonColor : Disabled, canBoost ? () => controller.Raid(NightOrder.Overload) : (Action)null);
                return;
            }
            Label(bottomPanel, "조용한 밤 · 무엇을 할까요?", 18, new Vector2(660, -22), new Vector2(490, 30), Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Activity(g, IndoorActivity.Craft, "장비 제작", "고철 -4", 660);
            Activity(g, IndoorActivity.Train, "수련", "경비 숙련", 782);
            Activity(g, IndoorActivity.Care, "돌보기", "사기 +6", 904);
            Activity(g, IndoorActivity.Overtime, "야간 특근", "자원 +, 분노 +6", 1026);
        }

        void Activity(Core.Game g, IndoorActivity activity, string name, string hint, float x)
        {
            bool can = g.CanDo(activity);
            Button(bottomPanel, $"{name}\n<size=14>{hint}</size>", 18, new Vector2(x, -96), new Vector2(116, 100), can ? ButtonColor : Disabled, can ? () => controller.Indoor(activity) : (Action)null);
        }

        public void HideModal() { Clear(modalLayer); }

        public void ShowEvent(Core.Game g, GameEvent e)
        {
            Clear(modalLayer);
            var box = ModalBox(980, 620);
            var speaker = g.Find(e.SpeakerId);
            var portrait = new GameObject("Portrait", typeof(RectTransform)).AddComponent<RawImage>();
            portrait.transform.SetParent(box, false);
            Place(portrait.rectTransform, new Vector2(28, -170), new Vector2(260, 280));
            portrait.texture = Portrait(e.SpeakerId);
            Label(box, speaker != null ? speaker.Name : "", 22, new Vector2(28, -336), new Vector2(260, 34), Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(box, speaker != null ? speaker.Title : "", 16, new Vector2(28, -364), new Vector2(260, 26), Quiet, TextAnchor.MiddleCenter);
            Label(box, e.Title, 34, new Vector2(320, -52), new Vector2(630, 56), e.Major ? Art.Red : Art.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(box, e.Body(g), 22, new Vector2(320, -194), new Vector2(630, 190), Ink, TextAnchor.UpperLeft);
            float y = -300;
            for (int i = 0; i < e.Options.Length; i++)
            {
                var option = e.Options[i];
                bool can = option.Available == null || option.Available(g);
                int index = i;
                Button(box, $"{option.Text}\n<size=15>{option.Hint}</size>", 20, new Vector2(320, y - 44), new Vector2(630, 80), can ? ButtonColor : Disabled, can ? () => controller.Choose(index) : (Action)null);
                y -= 88;
            }
        }

        public void ShowChronicle(string title, IEnumerable<ChronicleEntry> entries, string closeText, Action close)
        {
            Clear(modalLayer);
            var box = ModalBox(860, 620);
            Label(box, title, 34, new Vector2(36, -54), new Vector2(790, 56), Art.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            var list = entries.ToList();
            string body = list.Count == 0 ? "남은 기록이 없습니다." : string.Join("\n", list.Skip(Math.Max(0, list.Count - 11)).Select(c => "· " + c.Text));
            Label(box, body, 21, new Vector2(36, -300), new Vector2(790, 410), Ink, TextAnchor.UpperLeft);
            Button(box, closeText, 22, new Vector2(36, -566), new Vector2(790, 66), Selected, close);
        }

        public void ShowResult(string title, string body, string closeText, Action close)
        {
            Clear(modalLayer);
            var box = ModalBox(720, 320);
            Label(box, title, 32, new Vector2(36, -52), new Vector2(650, 54), Art.Amber, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(box, body, 22, new Vector2(36, -148), new Vector2(650, 120), Ink, TextAnchor.UpperLeft);
            Button(box, closeText, 22, new Vector2(36, -268), new Vector2(650, 64), Selected, close);
        }

        Transform ModalBox(float w, float h)
        {
            var dim = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
            dim.transform.SetParent(modalLayer, false);
            dim.color = new Color(0, 0, 0, 0.6f);
            Stretch(dim.rectTransform);
            var box = new GameObject("Box", typeof(RectTransform)).AddComponent<Image>();
            box.transform.SetParent(modalLayer, false);
            box.color = new Color(0.04f, 0.06f, 0.095f, 0.98f);
            var rect = box.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.85f, 1f, 0.55f);
            outline.effectDistance = new Vector2(2, -2);
            return box.transform;
        }

        GameObject MakePanel(string name, float ax, float ay, float bx, float by, Vector2 position, Vector2 size)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(canvas, false);
            image.color = Panel;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(ax, ay);
            rect.anchorMax = new Vector2(bx, by);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image.gameObject;
        }

        static void Place(RectTransform rect, Vector2 leftMiddle, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = leftMiddle;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        Image Image(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = new GameObject("Image", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            Place(image.rectTransform, position, size);
            return image;
        }

        Text Label(Transform parent, string text, int size, Vector2 position, Vector2 box, Color color, TextAnchor anchor, FontStyle style = FontStyle.Normal)
        {
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = Art.Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = style;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.1f;
            label.raycastTarget = false;
            Place(label.rectTransform, position, box);
            return label;
        }

        void Button(Transform parent, string text, int size, Vector2 position, Vector2 box, Color color, Action onClick)
        {
            var image = new GameObject("Button", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            Place(image.rectTransform, position, box);
            var button = image.gameObject.AddComponent<Button>();
            button.interactable = onClick != null;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.45f, 1.45f, 1.45f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1.2f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            var label = Label(image.transform, text, size, Vector2.zero, box, onClick != null ? Ink : new Color(0.42f, 0.46f, 0.52f), TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(-12, 0);
        }

        Texture2D Portrait(string id)
        {
            if (portraits.TryGetValue(id, out var cached)) return cached;
            const int size = 256;
            int seed = id.GetHashCode() & 255;
            Color accent = id == "yun" ? Art.Amber : id == "hanna" ? Art.Pink : id == "dohyun" ? Art.Lime : id == "seojin" ? Art.Cyan : id == "minjae" ? new Color(1f, 0.82f, 0.25f) : new Color(0.75f, 0.7f, 1f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float hair = id == "hanna" || id == "seojin" ? 0.13f : 0.03f;
            float headY = id == "minjae" ? 0.56f : 0.6f, headR = id == "minjae" ? 0.16f : 0.18f, shoulders = id == "oh" ? 0.27f : id == "dohyun" ? 0.42f : 0.34f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (size - 1f), v = y / (size - 1f);
                    float dx = u - 0.5f, dy = v - headY;
                    bool head = dx * dx / (headR * headR) + dy * dy / (headR * headR * 1.35f) < 1f;
                    bool hairShape = Mathf.Abs(dx) < headR + hair * 0.5f && v > headY - hair * 1.5f && v < headY + headR * 1.25f && dx * dx + (v - headY) * (v - headY) < (headR + 0.035f) * (headR + 0.035f) * 1.3f + hair * 0.08f;
                    bool body = v < 0.32f - dx * dx * 1.4f && Mathf.Abs(dx) < shoulders;
                    bool neck = Mathf.Abs(dx) < 0.07f && v < headY - headR * 0.8f && v > 0.28f;
                    Color c = Color.Lerp(new Color(0.02f, 0.03f, 0.05f), accent * 0.3f, Mathf.Clamp01(1.2f - v));
                    c.a = 1f;
                    if (hairShape || head || body || neck)
                    {
                        float lit = Mathf.Clamp01((dx + 0.18f) * 2.6f);
                        c = head && !(hairShape && v > headY + headR * 0.55f) ? Color.Lerp(new Color(0.04f, 0.04f, 0.06f), accent, Mathf.Pow(lit, 3f)) : new Color(0.025f, 0.03f, 0.045f);
                        float edge = Mathf.Sqrt(dx * dx + dy * dy / 1.35f);
                        if (head && edge > headR - 0.012f && dx > 0) c = accent * 1.1f;
                        if (body && dx > shoulders - 0.12f - 0.1f * v) c = Color.Lerp(c, accent, 0.7f);
                        c.a = 1f;
                    }
                    float noise = ((x * 7 + y * 13 + seed) % 11) / 11f * 0.022f;
                    c.r += noise; c.g += noise; c.b += noise;
                    texture.SetPixel(x, y, c);
                }
            texture.Apply();
            portraits[id] = texture;
            return texture;
        }
    }
}
