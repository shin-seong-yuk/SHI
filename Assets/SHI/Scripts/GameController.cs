using System.Collections;
using System.IO;
using System.Linq;
using SHI.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SHI.Client
{
    public sealed class GameController : MonoBehaviour
    {
        WorldView world;
        HudView hud;
        Camera cam;
        Core.Game game;
        BuildingType? placing;
        bool playing;
        string shotDir;

        void Awake()
        {
            cam = Camera.main;
            world = gameObject.AddComponent<WorldView>();
            hud = gameObject.AddComponent<HudView>();
            hud.Init(this);
            shotDir = ReadArg("-shiShots");
            if (!string.IsNullOrEmpty(shotDir)) Application.logMessageReceived += FailFast;
        }

        // A scripted verification run must stop at the first exception instead of hanging.
        void FailFast(string message, string stack, LogType type)
        {
            if (type != LogType.Exception) return;
            Directory.CreateDirectory(shotDir);
            File.WriteAllText(Path.Combine(shotDir, "exception.txt"), message + "\n" + stack);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(2);
#else
            Application.Quit(2);
#endif
        }

        void Start()
        {
            NewGame();
            if (!string.IsNullOrEmpty(shotDir)) StartCoroutine(AutoShots());
        }

        static string ReadArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        public void NewGame()
        {
            game = new Core.Game(System.Environment.TickCount);
            placing = null;
            playing = false;
            world.Init(game);
            hud.HideModal();
            hud.SetPlayVisible(true);
            Next();
        }

        void Next()
        {
            world.Sync();
            hud.Refresh(game, placing);
            if (game.IsOver) { ShowEnding(); return; }
            var current = game.CurrentEvent;
            if (current != null) hud.ShowEvent(game, current);
            else hud.HideModal();
        }

        void ShowEnding()
        {
            string title = game.Ending == EndReason.Survived ? "15일을 버텼다" : game.Ending == EndReason.Revolt ? "폭동으로 기지를 잃었다" : game.Ending == EndReason.NoPeople ? "남은 사람이 없다" : "모두가 희망을 잃었다";
            hud.ShowChronicle(title + " · 그동안의 기록", game.Chronicle, "처음부터 다시", NewGame);
        }

        public void CycleRole(Person person)
        {
            var roles = new[] { Role.Rest, Role.Store, Role.Scrap, Role.Food, Role.Battery, Role.Guard };
            int next = (System.Array.IndexOf(roles, person.Role) + 1) % roles.Length;
            game.Assign(person, roles[next]);
            Next();
        }

        public void ShowPerson(Person person) { hud.ShowPerson(game, person, Next); }

        public void SetRation(Ration ration) { game.Ration = ration; Next(); }
        public void ToggleShield() { game.SetShieldBoost(!game.ShieldBoost); Next(); }
        public void BeginPlacing(BuildingType type) { placing = type; Next(); }
        public void CancelPlacing() { placing = null; world.HideGhost(); Next(); }

        public void EndDay()
        {
            placing = null;
            world.HideGhost();
            game.EndDay();
            Next();
        }

        public void Indoor(IndoorActivity activity)
        {
            game.DoIndoor(activity);
            Next();
        }

        public void Raid(NightOrder order)
        {
            if (playing) return;
            StartCoroutine(RaidSequence(order));
        }

        IEnumerator RaidSequence(NightOrder order)
        {
            playing = true;
            hud.SetPlayVisible(false);
            int attack = game.AttackPower;
            var dayBefore = game.Day;
            var result = game.ResolveRaid(order);
            world.PlayRaid(result.Won, attack + 2, 6f);
            while (world.RaidPlaying) yield return null;
            hud.SetPlayVisible(true);
            playing = false;
            bool closed = false;
            hud.ShowResult(result.Won ? $"{dayBefore}일차 밤, 방어 성공" : $"{dayBefore}일차 밤, 방어 실패", $"{result.Summary}\n공격력 {result.Attack} : 방어력 {result.Defense}", "아침을 맞이한다", () => closed = true);
            while (!closed) yield return null;
            Next();
        }

        public void Choose(int index)
        {
            var current = game.CurrentEvent;
            game.Choose(index);
            if (current.Major)
            {
                hud.ShowChronicle("그날의 기록 · 이렇게 여기까지 왔다", game.ChronicleFor(current.CauseTag), "다시 일어선다", Next);
                hud.Refresh(game, placing);
                return;
            }
            Next();
        }

        void Update()
        {
            if (placing == null || cam == null || Mouse.current == null || !string.IsNullOrEmpty(shotDir)) return;
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector2 tile = Iso.ToTile(cam.ScreenToWorldPoint(screen));
            var info = Core.Game.Catalog[placing.Value];
            int x = Mathf.FloorToInt(tile.x - info.W / 2f + 0.5f), y = Mathf.FloorToInt(tile.y - info.D / 2f + 0.5f);
            bool valid = game.CanPlace(placing.Value, x, y);
            world.ShowGhost(placing.Value, x, y, valid);
            if (Mouse.current.leftButton.wasPressedThisFrame && !hud.PointerOverUi() && valid) PlaceAt(x, y);
        }

        void PlaceAt(int x, int y)
        {
            if (placing == null) return;
            if (game.Build(placing.Value, x, y))
            {
                placing = null;
                world.HideGhost();
                Next();
            }
        }

        // Scripted run used only with the -shiShots argument, to capture screens for verification.
        IEnumerator Shot(string name)
        {
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(shotDir, name + ".png"));
            yield return new WaitForSeconds(0.3f);
        }

        bool uiClick;

        // Sends a synthetic mouse click to the ration "절반" button through the input system.
        IEnumerator ClickTest()
        {
            var mouse = Mouse.current;
            if (mouse == null) yield break;
            var position = new Vector2(1622f / 1920f * Screen.width, (1080f - 290f) / 1080f * Screen.height);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            yield return null;
            uiClick = game.Ration == Ration.Reduced;
            game.Ration = Ration.Normal;
            Next();
        }

        IEnumerator AutoShots()
        {
            Directory.CreateDirectory(shotDir);
            game = new Core.Game(11);
            world.Init(game);
            Next();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("01_day1");
            yield return ClickTest();

            ShowPerson(game.People[0]);
            yield return Shot("01b_person_yun");
            Next();
            var crowded = new Core.Game(12);
            for (int i = 0; i < 5; i++) crowded.AddPerson("r" + i, "새 사람 " + i, "떠돌이", "길에서 받아들였다.", Role.Rest, 0, Temperaments.All[i * 5], i == 2);
            hud.Refresh(crowded, null);
            yield return Shot("01c_ten_people");
            Next();

            BeginPlacing(BuildingType.Workshop);
            yield return new WaitForSeconds(0.2f);
            world.ShowGhost(BuildingType.Workshop, 8, 8, true);
            yield return Shot("02_placing");
            PlaceAt(8, 8);

            bool strangerShot = false, signShot = false, actShot = false, nightShot = false, raidShot = false, eventShot = false, majorShot = false, nightDone = false;
            int guard = 0;
            while (!game.IsOver && guard++ < 200)
            {
                var current = game.CurrentEvent;
                if (current != null)
                {
                    if (!eventShot) { eventShot = true; yield return new WaitForSeconds(0.2f); yield return Shot("03_event"); }
                    if (current.Id == "yun_medicine") { yield return Shot("04_yun_medicine"); }
                    if (current.CountId == "영입" && !strangerShot) { strangerShot = true; yield return Shot("04b_stranger"); }
                    if (current.Id.StartsWith("sign:") && !signShot) { signShot = true; yield return Shot("04c_sign"); }
                    if (current.Id.StartsWith("act:") && !actShot) { actShot = true; yield return Shot("04d_act"); }
                    int option = current.Id == "yun_medicine" ? 2 : current.Id == "yun_ledger" ? 1 : current.Options.Select((o, i) => (o, i)).First(x => x.o.Available == null || x.o.Available(game)).i;
                    if (current.Major && !majorShot)
                    {
                        majorShot = true;
                        yield return Shot("05_major_" + current.Id);
                        Choose(option);
                        yield return Shot("06_chronicle");
                        Next();
                    }
                    else Choose(option);
                    yield return null;
                    continue;
                }
                if (game.Phase == Phase.Day)
                {
                    foreach (var p in game.People.Where(p => p.CanWork))
                    {
                        if (p.Id == "seojin") game.Assign(p, Role.Guard);
                        if (p.Id == "hanna") game.Assign(p, Role.Food);
                        if (p.Id == "minjae") game.Assign(p, Role.Scrap);
                        if (p.Id == "dohyun") game.Assign(p, Role.Guard);
                    }
                    game.Build(BuildingType.Wall, 2 + game.Day % 4 * 3, 11);
                    Next();
                    yield return null;
                    EndDay();
                    yield return new WaitForSeconds(1.4f);
                    continue;
                }
                if (game.RaidTonight)
                {
                    Raid(NightOrder.HoldWalls);
                    if (!raidShot)
                    {
                        raidShot = true;
                        yield return new WaitForSeconds(3.2f);
                        yield return Shot("07_raid");
                    }
                    while (playing) yield return null;
                    yield return new WaitForSeconds(0.3f);
                    if (game.Day > 0 && !nightDone) { nightDone = true; yield return Shot("08_raid_result"); }
                    Next();
                    yield return null;
                    continue;
                }
                if (!nightShot) { nightShot = true; yield return Shot("09_night"); }
                Indoor(IndoorActivity.Care);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            yield return Shot("10_end");
            File.WriteAllText(Path.Combine(shotDir, "summary.txt"), $"uiClick={uiClick} day={game.Day} ending={game.Ending} morale={game.Morale} anger={game.Anger} detection={game.Detection}\npeople={string.Join(",", game.People.Select(p => p.Name + ":" + p.Temper.Name + ":" + p.Health))}\nfired={string.Join(",", game.FiredEvents)}\n{string.Join("\n", game.Chronicle.Select(c => c.Text))}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }
    }
}
