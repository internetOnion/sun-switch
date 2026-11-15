using SunSwitch.LookAndFeel;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    public enum MenuScreen { Start, Pause, Settings, Controls, Confirmation, GameOver, Victory, Message }
    public enum MenuIntent { Play, Quit, Resume, Restart, MainMenu }
    public enum MenuCommandId { Play, Quit, Resume, Restart, MainMenu, Settings, Controls, Back, Cancel, Confirm }

    public readonly struct MenuResults
    {
        public readonly float Seconds;
        public readonly int Deaths, Shards, TotalShards;
        public MenuResults(float seconds, int deaths, int shards, int totalShards = 12)
        {
            Seconds = Mathf.Max(0, seconds); Deaths = Mathf.Max(0, deaths);
            TotalShards = Mathf.Max(1, totalShards); Shards = Mathf.Clamp(shards, 0, TotalShards);
        }
    }

    public sealed class StartMenuView : MonoBehaviour
    {
        [Serializable]
        public sealed class ScreenBinding
        {
            public MenuScreen id;
            public RectTransform root;
            public Button first;
            public CanvasGroup group;
        }
        [SerializeField] MenuFlowAdapter flow;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] List<ScreenBinding> screens = new List<ScreenBinding>();
        [SerializeField] Text confirmationHeading, confirmationBody, confirmationAction, messageHeading, messageBody;
        [SerializeField] Text gameOverStats, victoryStats;
        [SerializeField] MenuSettingsRow[] settings;
        readonly Stack<(MenuScreen screen, GameObject focus)> history = new Stack<(MenuScreen, GameObject)>();
        MenuScreen current;
        MenuIntent confirmedIntent;
        bool pending;
        Font font;
        public bool IsPreview => flow != null && flow.IsPreview;
        public MenuScreen CurrentScreen => current;
        static Color Ink => StorybookGraphic.Hex("282321");
        static Color Paper => StorybookGraphic.Hex("F1E5CA");

        void Awake()
        {
            if (canvasRoot == null) BuildCanvas();
            ConfigureSettings();
            HideQuitInBrowser();
            ShowStart();
        }
        // A web page can't close itself, so the Quit button would do nothing there.
        void HideQuitInBrowser()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer || canvasRoot == null) return;
            foreach (var action in canvasRoot.GetComponentsInChildren<MenuCommand>(true))
                if (action.command == MenuCommandId.Quit) action.gameObject.SetActive(false);
        }
        void Start() { Focus(Binding(current).first); }
        void Update()
        {
            if (canvasRoot == null) return;
            var group = Binding(current).group;
            group.alpha = Mathf.MoveTowards(group.alpha, 1, Time.unscaledDeltaTime / 0.15f);
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (history.Count > 0) Back();
            else if (current == MenuScreen.Pause) Dispatch(MenuIntent.Resume);
        }
        public void BindFlow(MenuFlowAdapter adapter)
        {
            flow = adapter;
            ConfigureSettings();
        }
        // Rows are in screen order: music, SFX, mouse sensitivity (BuildSettings).
        void ConfigureSettings()
        {
            if (settings == null) return;
            for (int i = 0; i < settings.Length; i++)
            {
                if (settings[i] == null) continue;
                settings[i].Configure(IsPreview);
                settings[i].Bind(flow, (MenuSetting)i);
            }
            if (flow == null || !flow.ProvidesSettings || canvasRoot == null) return;
            var settingsScreen = Binding(MenuScreen.Settings);
            if (settingsScreen == null) return;
            foreach (var label in settingsScreen.root.GetComponentsInChildren<Text>(true))
                if (label.name == "Provider status") label.text = "Changes apply straight away\nand are saved on this device.";
        }
        ScreenBinding Binding(MenuScreen id) => screens.Find(item => item.id == id);
        static void Focus(Button button) { if (Application.isPlaying && button != null && EventSystem.current != null) button.Select(); }
        void Activate(MenuScreen id)
        {
            current = id;
            var screen = Binding(id);
            screen.root.gameObject.SetActive(true); screen.root.SetAsLastSibling();
            screen.group.interactable = true; screen.group.blocksRaycasts = true;
            screen.group.alpha = Application.isPlaying ? 0 : 1;
            Focus(screen.first);
        }
        void Replace(MenuScreen id)
        {
            history.Clear(); CompleteRequest();
            foreach (var screen in screens) screen.root.gameObject.SetActive(false);
            Activate(id);
        }
        void Push(MenuScreen id, bool modal = false)
        {
            history.Push((current, EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null));
            var parent = Binding(current);
            parent.group.interactable = false; parent.group.blocksRaycasts = false;
            parent.group.alpha = 1;
            if (!modal) parent.root.gameObject.SetActive(false);
            Activate(id);
        }
        public void Back()
        {
            if (history.Count == 0) return;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(SfxId.UIClick);
            if (current == MenuScreen.Settings && flow != null && flow.ProvidesSettings) flow.SaveSettings();
            Binding(current).root.gameObject.SetActive(false);
            var previous = history.Pop(); Activate(previous.screen);
            if (previous.focus != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previous.focus);
        }
        public void ShowStart() => Replace(MenuScreen.Start);
        public void ShowPause() => Replace(MenuScreen.Pause);
        public void ShowResults(bool victory, MenuResults results)
        {
            // Same format as the HUD timer: mm:ss.hh, whole hundredths so 59.996 s never shows 60.00.
            int hundredths = Mathf.FloorToInt(results.Seconds * 100f);
            string time = $"{hundredths / 6000:00}:{hundredths / 100 % 60:00}.{hundredths % 100:00}";
            (victory ? victoryStats : gameOverStats).text = $"TIME     {time}\nDEATHS     {results.Deaths}\nSHARDS     {results.Shards} / {results.TotalShards}";
            Replace(victory ? MenuScreen.Victory : MenuScreen.GameOver);
        }
        public void Execute(MenuCommandId command)
        {
            if (pending) return;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(SfxId.UIClick);
            switch (command)
            {
                case MenuCommandId.Settings: Push(MenuScreen.Settings); break;
                case MenuCommandId.Controls: Push(MenuScreen.Controls); break;
                case MenuCommandId.Back:
                case MenuCommandId.Cancel: Back(); break;
                case MenuCommandId.Play: Dispatch(MenuIntent.Play); break;
                case MenuCommandId.Quit: Dispatch(MenuIntent.Quit); break;
                case MenuCommandId.Resume: Dispatch(MenuIntent.Resume); break;
                case MenuCommandId.Restart:
                    if (current == MenuScreen.Pause) Confirm(MenuIntent.Restart); else Dispatch(MenuIntent.Restart);
                    break;
                case MenuCommandId.MainMenu:
                    if (current == MenuScreen.Pause) Confirm(MenuIntent.MainMenu); else Dispatch(MenuIntent.MainMenu);
                    break;
                case MenuCommandId.Confirm: Back(); Dispatch(confirmedIntent); break;
            }
        }
        void Confirm(MenuIntent intent)
        {
            confirmedIntent = intent;
            confirmationAction.text = intent == MenuIntent.Restart ? "Restart Run" : "Leave Run";
            confirmationHeading.text = intent == MenuIntent.Restart ? "Restart this journey?" : "Leave this journey?";
            confirmationBody.text = intent == MenuIntent.Restart
                ? "Your current run will be reset.\nBegin again from the start?"
                : "Your current run will end.\nReturn to the main menu?";
            Push(MenuScreen.Confirmation, true);
        }
        void Dispatch(MenuIntent intent)
        {
            if (pending) return;
            if (flow == null) { ShowMessage("Action unavailable", "Game flow is not connected.\nThis menu is ready for integration."); return; }
            pending = true; Binding(current).group.interactable = false;
            flow.Request(intent, this);
        }
        public void CompleteRequest()
        {
            pending = false;
            if (screens.Count > 0) Binding(current).group.interactable = true;
        }
        public void ShowMessage(string heading, string message)
        {
            messageHeading.text = heading; messageBody.text = message;
            Push(MenuScreen.Message, true);
        }

        // Saved by the editor builder so each element can be edited in the Canvas hierarchy.
        public void BuildCanvas()
        {
            if (canvasRoot != null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvasRoot = Rect("Menu Canvas", transform, Vector2.zero, Vector2.zero);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            BuildStart(); BuildPause(); BuildSettings(); BuildControls(); BuildConfirmation(); BuildResults(false); BuildResults(true); BuildMessage();
            foreach (var screen in screens) screen.root.gameObject.SetActive(screen.id == MenuScreen.Start);
            current = MenuScreen.Start;
        }
        ScreenBinding Screen(MenuScreen id, bool scrim = false)
        {
            var root = Rect(id.ToString(), canvasRoot, Vector2.zero, Vector2.zero); Stretch(root);
            if (scrim) root.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.19f, 0.65f);
            var screen = new ScreenBinding { id = id, root = root, group = root.gameObject.AddComponent<CanvasGroup>() };
            screens.Add(screen); return screen;
        }
        void Backdrop(Transform parent, bool? victory = null, bool dayNight = false)
        {
            var sky = Art("Paper sky", parent, StorybookGraphic.Motif.Sky, Vector2.zero, Vector2.zero);
            sky.dayNightSplit = dayNight;
            if (victory.HasValue)
            {
                sky.skyBottom = StorybookGraphic.Hex(victory.Value ? "F1E5CA" : "B6CCE8");
                sky.skyTop = StorybookGraphic.Hex(victory.Value ? "E9B64F" : "424A78");
            }
            Stretch(sky.rectTransform);
            var frame = Art("Corner rules", parent, StorybookGraphic.Motif.Frame, Vector2.zero, Vector2.zero); Stretch(frame.rectTransform);
            if (dayNight) frame.gameObject.AddComponent<SplitTint>();
            var clouds = Art("Cloud banks", parent, StorybookGraphic.Motif.Clouds, Vector2.zero, new Vector2(0, 340)).rectTransform;
            clouds.anchorMin = Vector2.zero; clouds.anchorMax = new Vector2(1, 0); clouds.anchoredPosition = new Vector2(0, 90);
        }
        void BuildStart()
        {
            // Day on the left, night on the right: ink text turns cream where it crosses into the night.
            var screen = Screen(MenuScreen.Start); Backdrop(screen.root, null, true);
            var home = Rect("Centered start layout", screen.root, Vector2.zero, new Vector2(800, 960));
            Label("Eyebrow", home, "A LIGHTKEEPER'S JOURNEY", 18, new Vector2(0, 420), new Vector2(700, 32)).gameObject.AddComponent<SplitTint>();
            Art("Sun and moon crest", home, StorybookGraphic.Motif.Crest, new Vector2(0, 308), new Vector2(300, 180)).gameObject.AddComponent<SplitTint>();
            var title = Label("Title", home, "SUN SWITCH", 80, new Vector2(0, 175), new Vector2(780, 105));
            title.fontStyle = FontStyle.Bold; title.gameObject.AddComponent<SplitTint>();
            Label("Subtitle", home, "—   BETWEEN DAY & NIGHT   —", 18, new Vector2(0, 105), new Vector2(700, 32)).gameObject.AddComponent<SplitTint>();
            screen.first = Ribbon(home, "Play", 10, MenuCommandId.Play, true);
            Ribbon(home, "Settings", -78, MenuCommandId.Settings); Ribbon(home, "Controls", -166, MenuCommandId.Controls); Ribbon(home, "Quit", -254, MenuCommandId.Quit);
            Art("Introduction parchment", home, StorybookGraphic.Motif.Parchment, new Vector2(0, -374), new Vector2(770, 110));
            Label("Introduction", home, "The sun and moon have fallen out of sync.\nRestore the cycle.", 26, new Vector2(0, -374), new Vector2(730, 82));
            Label("Navigation hint", home, "↑ ↓  NAVIGATE     ENTER  SELECT", 16, new Vector2(0, -458), new Vector2(700, 28)).gameObject.AddComponent<SplitTint>();
        }
        RectTransform Card(ScreenBinding screen, string heading, Vector2 size)
        {
            var card = Art("Parchment card", screen.root, StorybookGraphic.Motif.Parchment, Vector2.zero, size).rectTransform;
            Label("Heading", card, heading, 46, new Vector2(0, size.y / 2 - 86), new Vector2(size.x - 80, 80)).fontStyle = FontStyle.Bold;
            Art("Gold divider", card, StorybookGraphic.Motif.Divider, new Vector2(0, size.y / 2 - 142), new Vector2(300, 28)); return card;
        }
        void BuildPause()
        {
            var screen = Screen(MenuScreen.Pause, true); var card = Card(screen, "A moment between worlds", new Vector2(860, 820));
            screen.first = Ribbon(card, "Resume", 150, MenuCommandId.Resume, true);
            Ribbon(card, "Settings", 62, MenuCommandId.Settings); Ribbon(card, "Controls", -26, MenuCommandId.Controls);
            Ribbon(card, "Restart Run", -114, MenuCommandId.Restart); Ribbon(card, "Main Menu", -202, MenuCommandId.MainMenu);
            Label("Pause hint", card, "ESC  RESUME", 16, new Vector2(0, -310), new Vector2(650, 28));
            Art("Left cloud corner", card, StorybookGraphic.Motif.CloudCorner, new Vector2(-284, -356), new Vector2(220, 90));
            Art("Right cloud corner", card, StorybookGraphic.Motif.CloudCorner, new Vector2(284, -356), new Vector2(220, 90));
        }
        void BuildSettings()
        {
            var screen = Screen(MenuScreen.Settings, true); var card = Card(screen, "Settings", new Vector2(860, 780));
            Label("Provider status", card, "Preview values only · changes are not saved.\nSettings remain disabled without a provider.", 24, new Vector2(0, 174), new Vector2(730, 76));
            settings = new MenuSettingsRow[3]; string[] names = { "Music volume", "SFX volume", "Mouse sensitivity" };
            for (int i = 0; i < 3; i++)
            {
                var row = Rect(names[i], card, new Vector2(0, 66 - i * 92), new Vector2(700, 88));
                Label("Label", row, names[i], 26, new Vector2(-185, 18), new Vector2(330, 40)).alignment = TextAnchor.MiddleLeft;
                var value = Label("Value", row, "Unavailable", 24, new Vector2(246, 18), new Vector2(205, 40)); value.alignment = TextAnchor.MiddleRight;
                var track = Rect("Slider", row, new Vector2(0, -22), new Vector2(680, 24));
                var background = Rect("Track", track, Vector2.zero, new Vector2(680, 5)); background.gameObject.AddComponent<Image>().color = StorybookGraphic.Hex("645B52");
                var area = Rect("Handle area", track, Vector2.zero, new Vector2(656, 24));
                var handle = Rect("Handle", area, Vector2.zero, new Vector2(20, 24)); var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Ink;
                var slider = track.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handleImage;
                slider.minValue = i == 2 ? 0.1f : 0; slider.maxValue = i == 2 ? 3 : 1; slider.value = i == 2 ? 1 : 0.75f;
                settings[i] = row.gameObject.AddComponent<MenuSettingsRow>(); settings[i].slider = slider; settings[i].valueLabel = value; settings[i].sensitivity = i == 2;
                slider.interactable = false;
            }
            screen.first = Ribbon(card, "Back", -282, MenuCommandId.Back);
        }
        void BuildControls()
        {
            var screen = Screen(MenuScreen.Controls, true); var card = Card(screen, "How to play", new Vector2(860, 760));
            string[] keys = { "W A S D", "MOUSE", "SPACE", "SHIFT", "RIGHT MOUSE", "ESC" };
            string[] actions = { "Move", "Look around", "Jump", "Sprint", "Switch day / night", "Pause" };
            for (int i = 0; i < keys.Length; i++)
            {
                float y = 172 - i * 67;
                var key = Rect("Keycap " + keys[i], card, new Vector2(-190, y), new Vector2(244, 50)); key.gameObject.AddComponent<Image>().color = Ink;
                bool mouse = i == 1 || i == 4;
                if (mouse) Art("Mouse icon", key, StorybookGraphic.Motif.Mouse, new Vector2(-94, 0), new Vector2(32, 44));
                Label("Key", key, keys[i], 21, new Vector2(mouse ? 18 : 0, 0), new Vector2(mouse ? 196 : 236, 46), Paper);
                Label("Action", card, actions[i], 27, new Vector2(160, y), new Vector2(360, 54)).alignment = TextAnchor.MiddleLeft;
            }
            screen.first = Ribbon(card, "Back", -272, MenuCommandId.Back);
        }
        void BuildConfirmation()
        {
            var screen = Screen(MenuScreen.Confirmation, true); var card = Card(screen, "Confirm", new Vector2(860, 570));
            confirmationHeading = card.Find("Heading").GetComponent<Text>();
            confirmationBody = Label("Consequence", card, "", 28, new Vector2(0, 18), new Vector2(730, 124));
            var confirm = Ribbon(card, "Confirm", -156, MenuCommandId.Confirm);
            confirmationAction = confirm.transform.Find("Label").GetComponent<Text>();
            confirm.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 72); confirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -156);
            var cancel = Ribbon(card, "Cancel", -156, MenuCommandId.Cancel, true);
            cancel.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 78); cancel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-180, -156);
            // Smaller controls keep their focus marker inside the ribbon.
            foreach (var button in new[] { confirm, cancel }) button.transform.Find("Focus marker").GetComponent<RectTransform>().anchoredPosition = new Vector2(-125, 0);
            screen.first = cancel;
        }
        void BuildResults(bool victory)
        {
            var screen = Screen(victory ? MenuScreen.Victory : MenuScreen.GameOver); Backdrop(screen.root, victory);
            var card = Card(screen, victory ? "The cycle is restored." : "Game Over", new Vector2(900, 870));
            var crest = Art("Celestial crest", card, StorybookGraphic.Motif.Crest, new Vector2(0, 164), new Vector2(230, 138));
            if (!victory) crest.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
            var stats = Label("Run results", card, "Waiting for run results", 28, new Vector2(0, -6), new Vector2(660, 168));
            if (victory) victoryStats = stats; else gameOverStats = stats;
            screen.first = Ribbon(card, victory ? "Play Again" : "Try Again", -174, MenuCommandId.Restart, true);
            Ribbon(card, "Main Menu", -270, MenuCommandId.MainMenu);
        }
        void BuildMessage()
        {
            var screen = Screen(MenuScreen.Message, true); var card = Card(screen, "Preview", new Vector2(860, 660));
            messageHeading = card.Find("Heading").GetComponent<Text>();
            messageBody = Label("Message", card, "", 28, Vector2.zero, new Vector2(730, 200));
            screen.first = Ribbon(card, "Back", -220, MenuCommandId.Back);
        }
        Button Ribbon(Transform parent, string caption, float y, MenuCommandId command, bool primary = false)
        {
            var graphic = Art(caption + " ribbon", parent, StorybookGraphic.Motif.Ribbon, new Vector2(0, y), new Vector2(primary ? 480 : 440, primary ? 78 : 72));
            graphic.color = Ink; graphic.raycastTarget = true;
            var button = graphic.gameObject.AddComponent<Button>(); button.targetGraphic = graphic;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Color.white; colors.selectedColor = Color.white;
            colors.pressedColor = StorybookGraphic.Hex("807057"); colors.fadeDuration = 0.15f; button.colors = colors;
            var label = Label("Label", graphic.transform, caption, 30, Vector2.zero, new Vector2(280, 64), Paper);
            var marker = Label("Focus marker", graphic.transform, "›", 34, new Vector2(-170, 0), new Vector2(35, 60), StorybookGraphic.Hex("E9B64F")); marker.gameObject.SetActive(false);
            var outline = graphic.gameObject.AddComponent<Outline>(); outline.effectColor = StorybookGraphic.Hex("E9B64F"); outline.effectDistance = new Vector2(2, -2); outline.enabled = false;
            var state = graphic.gameObject.AddComponent<MenuRibbon>(); state.label = label; state.marker = marker.gameObject; state.focusOutline = outline;
            if (primary) Art("Primary gold ornament", graphic.transform, StorybookGraphic.Motif.Divider, new Vector2(0, -28), new Vector2(220, 14));
            var action = graphic.gameObject.AddComponent<MenuCommand>(); action.view = this; action.command = command;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action.Invoke);
            else button.onClick.AddListener(action.Invoke);
#else
            button.onClick.AddListener(action.Invoke);
#endif
            return button;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        StorybookGraphic Art(string name, Transform parent, StorybookGraphic.Motif motif, Vector2 position, Vector2 size)
        {
            var art = Rect(name, parent, position, size).gameObject.AddComponent<StorybookGraphic>(); art.motif = motif; art.raycastTarget = false; return art;
        }
        Text Label(string name, Transform parent, string text, int size, Vector2 position, Vector2 dimensions, Color? color = null)
        {
            var label = Rect(name, parent, position, dimensions).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color ?? Ink; label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
    }
}
