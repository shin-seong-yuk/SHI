using System.Collections.Generic;
using System.Linq;
using SHI.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SHI.Client
{
    public sealed class WorldView : MonoBehaviour
    {
        sealed class BuildingView
        {
            public Building Model;
            public MeshRenderer Windows;
            public Light2D Light;
            public float Seed;
            public float BaseLight;
            public GameObject Warning;
        }

        sealed class PersonView
        {
            public Person Model;
            public Transform Root;
            public MeshRenderer Body;
            public Vector2 Tile;
            public Vector2 Target;
            public float Wait;
            public Light2D Lantern;
        }

        sealed class Robot
        {
            public Transform Root;
            public Vector2 Tile;
            public Vector2 Goal;
            public float Fall;
            public bool Dead;
        }

        readonly List<BuildingView> buildings = new List<BuildingView>();
        readonly List<PersonView> people = new List<PersonView>();
        readonly List<Robot> robots = new List<Robot>();
        readonly MeshBuilder builder = new MeshBuilder();
        MaterialPropertyBlock block;

        Transform world;
        Light2D globalLight;
        Game game;
        float night;
        float nightTarget;
        MeshRenderer domeRenderer;
        Light2D shieldLight;
        Transform drone;
        Light2D droneLight;
        float droneProgress = 2f;
        int droneDay = -1;
        Mesh rainMesh;
        Vector3[] rainDrops;
        MeshRenderer ghost;
        Mesh ghostMesh;
        Mesh tracerMesh;
        readonly List<Light2D> searchLights = new List<Light2D>();
        readonly List<(Vector3 a, Vector3 b, float life)> tracers = new List<(Vector3, Vector3, float)>();
        float raidTime;
        bool raidActive;
        bool raidWon;
        float raidLength;
        int domeRange = -1;

        public bool RaidPlaying => raidActive;

        void Awake() { block = new MaterialPropertyBlock(); }

        public void Init(Game newGame)
        {
            game = newGame;
            if (world != null) Destroy(world.gameObject);
            buildings.Clear();
            people.Clear();
            robots.Clear();
            searchLights.Clear();
            tracers.Clear();
            domeRange = -1;
            domeRenderer = null;
            shieldLight = null;
            droneDay = -1;
            droneProgress = 2f;
            raidActive = false;
            world = new GameObject("World").transform;
            world.SetParent(transform, false);

            globalLight = FindObjectsByType<Light2D>(FindObjectsSortMode.None).FirstOrDefault(l => l.lightType == Light2D.LightType.Global);
            if (globalLight == null)
            {
                globalLight = new GameObject("Global Light").AddComponent<Light2D>();
                globalLight.lightType = Light2D.LightType.Global;
                ApplyLayers(globalLight);
            }

            BuildGround();
            BuildRain();
            BuildDrone();
            BuildGhost();
            tracerMesh = new MeshBuilder().ToMesh();
            Art.MakeRenderer("Tracers", world, tracerMesh, Art.Glow, 4500);
            night = nightTarget = 0f;
            Sync();
        }

        static void ApplyLayers(Light2D light)
        {
            light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        }

        Light2D MakePointLight(string name, Transform parent, Vector3 position, Color color, float radius, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightInnerRadius = radius * 0.15f;
            light.pointLightOuterRadius = radius;
            light.color = color;
            light.intensity = intensity;
            light.falloffIntensity = 0.75f;
            light.shadowsEnabled = false;
            ApplyLayers(light);
            return light;
        }

        void BuildGround()
        {
            builder.Clear();
            for (int x = -7; x < 19; x++)
                for (int y = -7; y < 19; y++)
                {
                    bool inside = x >= 0 && x < Game.GridSize && y >= 0 && y < Game.GridSize;
                    float n = Mathf.PerlinNoise(x * 0.37f + 3f, y * 0.37f + 9f), v = Hash(x, y);
                    Color c = inside ? Color.Lerp(new Color(0.36f, 0.36f, 0.38f), new Color(0.43f, 0.42f, 0.41f), v) : Color.Lerp(new Color(0.20f, 0.22f, 0.25f), new Color(0.27f, 0.27f, 0.28f), n);
                    builder.Tile(x + 0.01f, y + 0.01f, 0.98f, 0.98f, 0f, c);
                    if (v > 0.8f) builder.Tile(x + v * 0.4f, y + 0.25f, 0.22f, 0.07f, 0.001f, c * 0.6f);
                }
            Art.MakeRenderer("Ground", world, builder.ToMesh(new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }), Art.Lit, 0);

            float[,] ruins = { { -5, -4, 3, 3, 3.1f }, { 0, -6, 4, 3, 2.1f }, { 7, -6, 3, 3, 2.8f }, { 13, -4, 3, 4, 1.7f }, { -6, 3, 3, 3, 1.3f }, { -6, 9, 3, 3, 2.0f }, { 14, 3, 2.5f, 3, 0.9f }, { 14, 9, 3, 3, 1.1f }, { 2, 14, 3, 2, 0.5f }, { 9, 14, 3, 2, 0.6f } };
            for (int i = 0; i < ruins.GetLength(0); i++)
            {
                float x = ruins[i, 0], y = ruins[i, 1], w = ruins[i, 2], d = ruins[i, 3], h = ruins[i, 4];
                builder.Clear();
                Color wall = new Color(0.31f, 0.34f, 0.40f);
                builder.Box(x, y, w, d, 0, h * 0.85f, wall);
                builder.Box(x, y, w * 0.55f, d * 0.6f, h * 0.85f, h * 0.15f, wall);
                Art.MakeRenderer("Ruin", world, builder.ToMesh(), Art.Lit, Iso.Order(x + w / 2, y + d / 2));
                builder.Clear();
                int floors = Mathf.Max(1, Mathf.RoundToInt(h * 2.6f));
                for (int f = 0; f < floors; f++)
                    for (int c = 0; c < Mathf.RoundToInt(w * 2.5f); c++)
                    {
                        float v = Hash(i * 11 + c, f);
                        float a = x + (c + 0.25f) / (w * 2.5f) * w, b = a + 0.5f / (w * 2.5f) * w, z = (f + 0.3f) / floors * h * 0.8f, z2 = z + 0.45f / floors * h * 0.8f;
                        builder.FrontPanel(a, b, y + d, z, z2, v > 0.82f ? Art.Amber * 0.6f : new Color(0.012f, 0.014f, 0.02f, 1f));
                    }
                Art.MakeRenderer("RuinWindows", world, builder.ToMesh(), Art.Glow, Iso.Order(x + w / 2, y + d / 2) + 1);
            }

            builder.Clear();
            Color fence = new Color(0.42f, 0.45f, 0.51f);
            for (int i = 0; i < Game.GridSize; i++)
            {
                if (i % 3 != 2) builder.Box(i, -0.14f, 1, 0.1f, 0, 0.16f, fence);
                if (i % 4 != 3) builder.Box(-0.14f, i, 0.1f, 1, 0, 0.16f, fence);
            }
            Art.MakeRenderer("FenceBack", world, builder.ToMesh(), Art.Lit, 990);
            builder.Clear();
            for (int i = 0; i < Game.GridSize; i++)
            {
                if (i != 5 && i != 6) builder.Box(i, 12.04f, 1, 0.1f, 0, 0.16f, fence);
                if (i % 4 != 3) builder.Box(12.04f, i, 0.1f, 1, 0, 0.16f, fence);
            }
            Art.MakeRenderer("FenceFront", world, builder.ToMesh(), Art.Lit, 1490);

            searchLights.Add(MakePointLight("Searchlight A", world, Iso.P(13f, 6f), new Color(1f, 0.95f, 0.72f), 1.6f, 0f));
            searchLights.Add(MakePointLight("Searchlight B", world, Iso.P(6f, 13f), new Color(1f, 0.95f, 0.72f), 1.6f, 0f));
        }

        static float Hash(float a, float b)
        {
            float s = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.547f;
            return s - Mathf.Floor(s);
        }

        void BuildRain()
        {
            rainDrops = new Vector3[140];
            for (int i = 0; i < rainDrops.Length; i++) rainDrops[i] = new Vector3(Random.Range(-11f, 11f), Random.Range(-9f, 6f));
            rainMesh = new MeshBuilder().ToMesh();
            Art.MakeRenderer("Rain", world, rainMesh, Art.Glow, 6000);
        }

        void BuildDrone()
        {
            builder.Clear();
            builder.Rect2D(Vector3.zero, 0.34f, 0.07f, new Color(0.08f, 0.09f, 0.11f));
            builder.Rect2D(new Vector3(-0.22f, 0.05f), 0.16f, 0.025f, new Color(0.18f, 0.19f, 0.22f));
            builder.Rect2D(new Vector3(0.22f, 0.05f), 0.16f, 0.025f, new Color(0.18f, 0.19f, 0.22f));
            drone = new GameObject("Drone").transform;
            drone.SetParent(world, false);
            Art.MakeRenderer("Body", drone, builder.ToMesh(), Art.Lit, 5200);
            builder.Clear();
            builder.Rect2D(new Vector3(0, -0.04f), 0.05f, 0.05f, Art.Red);
            Art.MakeRenderer("Eye", drone, builder.ToMesh(), Art.Glow, 5201);
            droneLight = MakePointLight("Spot", drone, Vector3.zero, Art.Red, 1.2f, 0f);
            drone.gameObject.SetActive(false);
        }

        void BuildGhost()
        {
            ghostMesh = new MeshBuilder().ToMesh();
            ghost = Art.MakeRenderer("Ghost", world, ghostMesh, Art.Glow, 5100);
            ghost.enabled = false;
        }

        public void ShowGhost(BuildingType type, int x, int y, bool valid)
        {
            var info = Game.Catalog[type];
            builder.Clear();
            Color c = valid ? Art.Lime * 0.35f : Art.Red * 0.35f;
            builder.Tile(x, y, info.W, info.D, 0.002f, c);
            builder.Box(x + 0.05f, y + 0.05f, info.W - 0.1f, info.D - 0.1f, 0f, 0.28f, c * 0.5f);
            builder.ToMesh(ghostMesh);
            ghost.enabled = true;
        }

        public void HideGhost() { if (ghost != null) ghost.enabled = false; }

        public void Sync()
        {
            foreach (var model in game.Buildings)
                if (!buildings.Any(v => v.Model == model)) CreateBuilding(model);
            foreach (var v in buildings) if (v.Warning != null) v.Warning.SetActive(v.Model.Exposed);
            foreach (var model in game.People)
                if (!people.Any(v => v.Model == model)) CreatePerson(model);
            nightTarget = game.Phase == Phase.Night || game.Phase == Phase.End ? 1f : 0f;
            RetargetPeople();
            if (game.DroneSeen && game.Phase == Phase.Day && droneDay != game.Day) { droneDay = game.Day; droneProgress = 0f; }
            if (domeRange != game.ShieldRange) BuildDome();
        }

        public void ForceNight(bool value) { if (value) nightTarget = 1f; }

        void BuildDome()
        {
            domeRange = game.ShieldRange;
            var shield = game.Buildings.First(b => b.Type == BuildingType.Shield);
            Vector3 c = Iso.P(shield.X + 1, shield.Y + 1);
            float rx = domeRange * 0.7071f, ry = domeRange * 0.3536f, dh = domeRange * 0.62f;
            builder.Clear();
            builder.Fan(c, rx, ry, 0, Mathf.PI * 2, 64, Art.Cyan * 0.05f);
            builder.Fan(c, rx, dh, 0, Mathf.PI, 48, Art.Cyan * 0.035f);
            builder.Ring(c, rx, ry, 0, Mathf.PI * 2, 96, 0.012f, Art.Cyan * 0.8f);
            builder.Ring(c, rx, dh, 0, Mathf.PI, 64, 0.008f, Art.Cyan * 0.45f);
            if (domeRenderer != null) Destroy(domeRenderer.gameObject);
            domeRenderer = Art.MakeRenderer("Shield Dome", world, builder.ToMesh(), Art.Glow, 5000);
            if (shieldLight == null) shieldLight = MakePointLight("Shield Light", world, c + Vector3.up * 0.4f, Art.Cyan, 3.6f, 1f);
        }

        void CreateBuilding(Building b)
        {
            float x = b.X, y = b.Y, w = b.W, d = b.D;
            var root = new GameObject(b.Type.ToString()).transform;
            root.SetParent(world, false);
            int order = Iso.Order(x + w / 2, y + d / 2);
            var view = new BuildingView { Model = b, Seed = Random.value * 10f };
            Color wall, winColor, sign = Color.white;
            float h;
            string label = null;
            switch (b.Type)
            {
                case BuildingType.Shield: wall = new Color(0.33f, 0.45f, 0.55f); winColor = Art.Cyan; h = 0.42f; break;
                case BuildingType.Warehouse: wall = new Color(0.36f, 0.48f, 0.54f); winColor = new Color(0.5f, 0.92f, 1f); h = 0.62f; label = "창고"; sign = Art.Cyan; break;
                case BuildingType.Quarters: wall = new Color(0.52f, 0.44f, 0.44f); winColor = Art.Amber; h = 0.74f; label = "숙소"; sign = Art.Amber; break;
                case BuildingType.Clinic: wall = new Color(0.56f, 0.44f, 0.56f); winColor = new Color(1f, 0.6f, 0.85f); h = 0.68f; label = "의무실"; sign = Art.Pink; break;
                case BuildingType.Workshop: wall = new Color(0.52f, 0.47f, 0.34f); winColor = new Color(1f, 0.82f, 0.4f); h = 0.7f; label = "작업장"; sign = new Color(1f, 0.82f, 0.25f); break;
                case BuildingType.Farm: wall = new Color(0.34f, 0.52f, 0.40f); winColor = Art.Lime; h = 0.42f; label = "배양실"; sign = Art.Lime; break;
                default: wall = new Color(0.46f, 0.49f, 0.55f); winColor = Art.Red; h = 0.34f; break;
            }

            builder.Clear();
            builder.Box(x, y, w, d, 0, h, wall);
            if (b.Type == BuildingType.Shield)
            {
                builder.Box(x + 0.72f, y + 0.72f, 0.56f, 0.56f, h, 0.42f, new Color(0.28f, 0.35f, 0.42f));
                builder.Box(x + 0.92f, y + 0.92f, 0.16f, 0.16f, h + 0.42f, 0.5f, new Color(0.4f, 0.46f, 0.52f));
            }
            else if (b.Type == BuildingType.Workshop)
            {
                builder.Box(x + w - 0.6f, y + 0.25f, 0.28f, 0.28f, h, 0.42f, new Color(0.3f, 0.26f, 0.22f));
            }
            else if (b.Type == BuildingType.Quarters)
            {
                builder.Box(x + 0.2f, y + 0.2f, w - 0.4f, d - 0.4f, h, 0.08f, MeshBuilder.Shade(wall, 0.8f));
            }
            else if (b.Type != BuildingType.Wall)
            {
                builder.Box(x + 0.25f, y + 0.25f, 0.42f, 0.32f, h, 0.14f, MeshBuilder.Shade(wall, 0.72f));
            }
            if (b.Type == BuildingType.Warehouse) builder.FrontPanel(x + 0.7f, x + 2.3f, y + d + 0.001f, 0f, 0.36f, new Color(0.16f, 0.2f, 0.24f));
            Art.MakeRenderer("Body", root, builder.ToMesh(), Art.Lit, order);

            builder.Clear();
            if (b.Type != BuildingType.Wall && b.Type != BuildingType.Shield)
            {
                int columns = Mathf.RoundToInt(w * 3f), rows = Mathf.Max(1, Mathf.FloorToInt(h / 0.24f));
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                    {
                        if (b.Type == BuildingType.Warehouse && r == 0 && c > 1 && c < columns - 2) continue;
                        bool on = Hash(c + b.X * 3, r + b.Y * 5) > 0.3f;
                        float a = x + (c + 0.22f) / columns * w, a2 = a + 0.56f / columns * w, z = (r + 0.32f) / rows * h, z2 = z + 0.4f / rows * h;
                        builder.FrontPanel(a, a2, y + d + 0.002f, z, z2, on ? winColor * 0.85f : new Color(0.02f, 0.025f, 0.035f));
                    }
                int sideColumns = Mathf.RoundToInt(d * 2f);
                for (int c = 0; c < sideColumns; c++)
                {
                    bool on = Hash(c + b.X * 7, b.Y) > 0.45f;
                    float a = y + (c + 0.25f) / sideColumns * d, a2 = a + 0.5f / sideColumns * d;
                    builder.SidePanel(x + w + 0.002f, a, a2, h * 0.45f, h * 0.75f, on ? winColor * 0.65f : new Color(0.02f, 0.025f, 0.035f));
                }
            }
            if (b.Type == BuildingType.Shield)
            {
                builder.Rect2D(Iso.P(x + 1, y + 1, h + 0.97f), 0.1f, 0.1f, Art.Cyan);
                builder.Ring(Iso.P(x + 1, y + 1, h + 0.5f), 0.22f, 0.1f, 0, Mathf.PI * 2, 24, 0.012f, Art.Cyan);
            }
            if (b.Type == BuildingType.Farm)
            {
                for (int k = 0; k < 3; k++) builder.Tile(x + 0.2f, y + 0.25f + k * 0.92f, w - 0.4f, 0.62f, h + 0.005f, Art.Lime * 0.4f);
            }
            if (b.Type == BuildingType.Clinic)
            {
                Vector3 cc = Iso.P(x + w / 2, y + d, h * 0.55f);
                builder.Rect2D(cc, 0.3f, 0.08f, Art.Pink);
                builder.Rect2D(cc, 0.08f, 0.3f, Art.Pink);
            }
            if (b.Type == BuildingType.Wall)
            {
                builder.FrontPanel(x + 0.1f, x + w - 0.1f, y + d + 0.002f, h * 0.7f, h * 0.84f, Art.Red * 0.75f);
            }
            view.Windows = Art.MakeRenderer("Windows", root, builder.ToMesh(), Art.Glow, order + 1);

            if (label != null)
            {
                var text = new GameObject("Sign").AddComponent<TextMesh>();
                text.transform.SetParent(root, false);
                text.transform.position = Iso.P(x + w * 0.5f, y + d * 0.5f, h + 0.32f);
                text.font = Art.Font;
                text.text = label;
                text.fontSize = 64;
                text.characterSize = 0.045f;
                text.anchor = TextAnchor.MiddleCenter;
                text.fontStyle = FontStyle.Bold;
                text.color = sign;
                var textRenderer = text.GetComponent<MeshRenderer>();
                textRenderer.sharedMaterial = Art.Font.material;
                textRenderer.sortingOrder = order + 2;
            }

            view.BaseLight = b.Type == BuildingType.Wall ? 0.35f : 1.05f;
            view.Light = MakePointLight("Light", root, Iso.P(x + w * 0.65f, y + d + 0.45f, 0.1f), winColor, Mathf.Max(w, d) * 0.95f + 0.5f, 0f);

            builder.Clear();
            builder.Ring(Iso.P(x + w / 2, y + d / 2, 0.003f), (w + d) * 0.42f, (w + d) * 0.21f, 0, Mathf.PI * 2, 32, 0.012f, Art.Red);
            var warning = Art.MakeRenderer("Exposed", root, builder.ToMesh(), Art.Glow, order - 1);
            view.Warning = warning.gameObject;
            view.Warning.SetActive(b.Exposed);
            buildings.Add(view);
        }

        void CreatePerson(Person model)
        {
            var root = new GameObject(model.Name).transform;
            root.SetParent(world, false);
            builder.Clear();
            Color cloth = model.Id == "yun" ? new Color(0.74f, 0.72f, 0.48f) : model.Id == "hanna" ? new Color(0.92f, 0.66f, 0.8f) : model.Id == "dohyun" ? new Color(0.5f, 0.68f, 0.5f) : model.Id == "seojin" ? new Color(0.45f, 0.68f, 0.95f) : model.Id == "minjae" ? new Color(0.95f, 0.72f, 0.36f) : new Color(0.72f, 0.72f, 0.74f);
            builder.Rect2D(new Vector3(0, 0.1f), 0.1f, 0.2f, cloth);
            builder.Fan(new Vector3(0, 0.24f), 0.055f, 0.055f, 0, Mathf.PI * 2, 12, new Color(0.96f, 0.82f, 0.68f));
            builder.Fan(new Vector3(0.015f, 0f), 0.09f, 0.035f, 0, Mathf.PI * 2, 12, new Color(0f, 0f, 0f, 0.5f));
            var body = Art.MakeRenderer("Body", root, builder.ToMesh(), Art.Lit, 1200);
            var view = new PersonView { Model = model, Root = root, Body = body, Tile = RoleAnchor(model) + Jitter(), Lantern = MakePointLight("Lantern", root, Vector3.zero, Art.Amber, 0.75f, 0f) };
            view.Target = view.Tile;
            people.Add(view);
        }

        Vector2 Jitter() => new Vector2(Random.Range(-0.7f, 0.7f), Random.Range(-0.7f, 0.7f));

        Vector2 RoleAnchor(Person p)
        {
            if (p.Health == Health.Injured || p.Frail) return AnchorOf(game.Has(BuildingType.Clinic) ? BuildingType.Clinic : BuildingType.Quarters);
            switch (p.Role)
            {
                case Role.Store: return AnchorOf(BuildingType.Warehouse);
                case Role.Scrap: return new Vector2(2f, 13.2f);
                case Role.Food: return new Vector2(9.5f, 13.2f);
                case Role.Battery: return new Vector2(13.2f, 8f);
                case Role.Guard: return new Vector2(11f, 11f);
                default: return AnchorOf(BuildingType.Quarters);
            }
        }

        Vector2 AnchorOf(BuildingType type)
        {
            var b = game.Buildings.FirstOrDefault(o => o.Type == type) ?? game.Buildings[0];
            return new Vector2(b.X + b.W / 2f, b.Y + b.D + 0.5f);
        }

        void RetargetPeople()
        {
            foreach (var v in people)
            {
                bool visible = v.Model.Present && (game.Phase == Phase.Day || v.Model.Role == Role.Guard);
                v.Root.gameObject.SetActive(visible);
                v.Target = RoleAnchor(v.Model) + Jitter();
            }
        }

        public void PlayRaid(bool won, int attack, float seconds)
        {
            foreach (var r in robots) Destroy(r.Root.gameObject);
            robots.Clear();
            int count = Mathf.Clamp(attack, 5, 14);
            for (int i = 0; i < count; i++)
            {
                bool side = i % 2 == 0;
                var start = side ? new Vector2(18f + Random.value * 3f, Random.Range(1f, 11f)) : new Vector2(Random.Range(1f, 11f), 18f + Random.value * 3f);
                var goal = side ? new Vector2(won ? 13.2f : 11f, start.y) : new Vector2(start.x, won ? 13.2f : 11f);
                robots.Add(new Robot { Root = CreateRobot(), Tile = start, Goal = goal });
            }
            raidWon = won;
            raidActive = true;
            raidTime = 0f;
            raidLength = seconds;
            nightTarget = 1f;
            night = 1f;
            foreach (var v in people) if (v.Model.Role == Role.Guard && v.Model.Present) { v.Root.gameObject.SetActive(true); v.Target = new Vector2(Random.Range(9f, 11.5f), Random.Range(9f, 11.5f)); }
        }

        Transform CreateRobot()
        {
            var root = new GameObject("Robot").transform;
            root.SetParent(world, false);
            builder.Clear();
            builder.Rect2D(new Vector3(0, 0.17f), 0.24f, 0.34f, new Color(0.13f, 0.14f, 0.17f));
            builder.Rect2D(new Vector3(0, 0.38f), 0.16f, 0.12f, new Color(0.19f, 0.2f, 0.24f));
            builder.Rect2D(new Vector3(-0.15f, 0.2f), 0.05f, 0.22f, new Color(0.1f, 0.1f, 0.13f));
            builder.Rect2D(new Vector3(0.15f, 0.2f), 0.05f, 0.22f, new Color(0.1f, 0.1f, 0.13f));
            Art.MakeRenderer("Body", root, builder.ToMesh(), Art.Lit, 1300);
            builder.Clear();
            builder.Rect2D(new Vector3(-0.04f, 0.385f), 0.04f, 0.028f, Art.Red);
            builder.Rect2D(new Vector3(0.04f, 0.385f), 0.04f, 0.028f, Art.Red);
            Art.MakeRenderer("Eyes", root, builder.ToMesh(), Art.Glow, 1301);
            MakePointLight("Eye Light", root, new Vector3(0, 0.38f), Art.Red, 0.65f, 1.4f);
            return root;
        }

        void Update()
        {
            if (game == null || world == null) return;
            float dt = Time.deltaTime, t = Time.time;
            night = Mathf.MoveTowards(night, nightTarget, dt * 0.8f);
            UpdateLighting(t);
            UpdatePeople(dt);
            UpdateDrone(dt, t);
            UpdateRain(dt);
            UpdateRaid(dt, t);
        }

        void UpdateLighting(float t)
        {
            globalLight.color = Color.Lerp(new Color(1f, 0.9f, 0.78f), new Color(0.34f, 0.42f, 0.68f), night);
            globalLight.intensity = Mathf.Lerp(0.95f, 0.42f, night);
            foreach (var v in buildings)
            {
                float flick = 0.86f + 0.14f * Mathf.PerlinNoise(t * 1.7f, v.Seed);
                block.SetFloat("_Intensity", Mathf.Lerp(0.35f, 1.6f, night) * flick);
                v.Windows.SetPropertyBlock(block);
                v.Light.intensity = v.BaseLight * Mathf.Lerp(0.05f, 1f, night) * flick;
            }
            if (domeRenderer != null)
            {
                float pulse = 0.8f + 0.2f * Mathf.Sin(t * 1.8f);
                block.SetFloat("_Intensity", Mathf.Lerp(0.6f, 1.15f, night) * pulse);
                domeRenderer.SetPropertyBlock(block);
                shieldLight.intensity = Mathf.Lerp(0.2f, 1.25f, night) * pulse;
            }
            for (int i = 0; i < searchLights.Count; i++)
            {
                var s = searchLights[i];
                s.intensity = raidActive ? 1.8f : 0f;
                if (raidActive) s.transform.position = i == 0 ? Iso.P(14f + 2.4f * Mathf.Sin(t * 0.9f), 6f + 5f * Mathf.Cos(t * 0.6f)) : Iso.P(6f + 5f * Mathf.Sin(t * 0.7f + 1f), 14f + 2.4f * Mathf.Cos(t * 1.1f));
            }
        }

        void UpdatePeople(float dt)
        {
            foreach (var v in people)
            {
                if (!v.Root.gameObject.activeSelf) continue;
                v.Wait -= dt;
                if (v.Wait <= 0f && Vector2.Distance(v.Tile, v.Target) < 0.05f)
                {
                    v.Wait = Random.Range(0.8f, 3f);
                    v.Target = RoleAnchor(v.Model) + Jitter();
                }
                float speed = v.Model.Health == Health.Injured ? 0.25f : 0.9f;
                v.Tile = Vector2.MoveTowards(v.Tile, v.Target, speed * dt);
                bool moving = Vector2.Distance(v.Tile, v.Target) > 0.05f;
                v.Root.position = Iso.P(v.Tile.x, v.Tile.y, moving ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.012f : 0f);
                v.Body.sortingOrder = Iso.Order(v.Tile.x, v.Tile.y);
                v.Lantern.intensity = night * 0.9f;
            }
        }

        void UpdateDrone(float dt, float t)
        {
            bool flying = droneProgress < 1f;
            drone.gameObject.SetActive(flying);
            if (!flying) return;
            droneProgress += dt / 9f;
            drone.position = Vector3.Lerp(Iso.P(-4f, 8f, 1.4f), Iso.P(18f, 2f, 1.4f), droneProgress) + Vector3.up * Mathf.Sin(t * 3f) * 0.05f;
            droneLight.intensity = (Mathf.Sin(t * 10f) > 0f ? 1.5f : 0.3f);
        }

        void UpdateRain(float dt)
        {
            builder.Clear();
            var color = new Color(0.45f, 0.62f, 0.85f) * Mathf.Lerp(0.16f, 0.24f, night);
            for (int i = 0; i < rainDrops.Length; i++)
            {
                var p = rainDrops[i] + new Vector3(-1.4f, -8f) * dt;
                if (p.y < -9f) p = new Vector3(Random.Range(-10f, 13f), 6f);
                rainDrops[i] = p;
                builder.Quad(p, p + new Vector3(0.012f, 0), p + new Vector3(0.05f, 0.24f), p + new Vector3(0.038f, 0.24f), color);
            }
            builder.ToMesh(rainMesh);
        }

        void UpdateRaid(float dt, float t)
        {
            for (int i = tracers.Count - 1; i >= 0; i--)
            {
                var tr = tracers[i];
                tr.life -= dt * 4f;
                if (tr.life <= 0f) tracers.RemoveAt(i); else tracers[i] = tr;
            }
            builder.Clear();
            foreach (var tr in tracers)
            {
                Vector3 n = Vector3.Cross(tr.b - tr.a, Vector3.forward).normalized * 0.012f;
                builder.Quad(tr.a - n, tr.a + n, tr.b + n, tr.b - n, Art.Amber * tr.life);
            }
            builder.ToMesh(tracerMesh);

            if (!raidActive) return;
            raidTime += dt;
            foreach (var r in robots)
            {
                if (r.Dead)
                {
                    r.Fall += dt * 2f;
                    r.Root.localScale = new Vector3(1f, Mathf.Clamp01(1f - r.Fall), 1f);
                    continue;
                }
                r.Tile = Vector2.MoveTowards(r.Tile, r.Goal, dt * 1.7f);
                r.Root.position = Iso.P(r.Tile.x, r.Tile.y, Mathf.Abs(Mathf.Sin(t * 7f + r.Goal.x)) * 0.02f);
                foreach (var rend in r.Root.GetComponentsInChildren<MeshRenderer>())
                    rend.sortingOrder = Iso.Order(r.Tile.x, r.Tile.y) + (rend.name == "Eyes" ? 1 : 0);
            }
            var guards = people.Where(p => p.Root.gameObject.activeSelf && p.Model.Role == Role.Guard).ToList();
            var living = robots.Where(r => !r.Dead).ToList();
            if (living.Count > 0 && raidTime > 1f && Random.value < dt * 7f)
            {
                var target = living[Random.Range(0, living.Count)];
                Vector3 from = guards.Count > 0 ? guards[Random.Range(0, guards.Count)].Root.position + Vector3.up * 0.15f : Iso.P(11f, 11f, 0.2f);
                tracers.Add((from, target.Root.position + Vector3.up * 0.2f, 1f));
                if (raidWon && raidTime > raidLength * 0.45f && Random.value < 0.45f) target.Dead = true;
            }
            if (raidTime >= raidLength)
            {
                raidActive = false;
                foreach (var r in robots) Destroy(r.Root.gameObject);
                robots.Clear();
            }
        }
    }
}
