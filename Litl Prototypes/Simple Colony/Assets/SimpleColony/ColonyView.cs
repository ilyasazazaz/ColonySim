using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SimpleColony
{
    public sealed class ColonyView : MonoBehaviour
    {
        World world;
        [SerializeField] GameObject generatedInterface, generatedInput;
        [SerializeField] int sessionSeed = 4271;
        MapGraphic map;
        RectTransform safeArea;
        RectTransform mapViewport;
        RectTransform topPanel, selectionPanel, colonyControlsRect;
        CanvasScaler canvasScaler;
        bool portraitLayout, layoutReady;
        Text caption, description, details, stocks, diagnosticText, feedback, speedText, recruitRoleText;
        DescriptionPopup descriptionPopup;
        readonly RuntimeStatistics statistics = new RuntimeStatistics();
        readonly System.Collections.Generic.List<Button> researchButtons = new System.Collections.Generic.List<Button>();
        readonly System.Collections.Generic.List<Text> researchPrices = new System.Collections.Generic.List<Text>();
        Image recruitPortrait;
        UiIcon playIcon;
        Image portrait;
        GameObject help, diagnostics, pawnActions, nodeActions, coreActions, recruitment, research, workforce;
        Button convert, accessButton, recruitButton;
        Font font;
        Sprite disc;
        Texture2D discTexture;
        float accumulator, refresh, tickMs;
        bool paused;
        // Zero selects unrestricted simulation within the per-frame CPU budget.
        static readonly int[] Speeds = { 1, 2, 5, 10, 25, 50, 100, 0 };
        int speedIndex;
        Profession recruitRole = Profession.Unemployed;
        Profession chosenRole;
        public int SelectedNode { get; private set; }
        public int SelectedPawn { get; private set; } = -1;
        public bool MapInteractive => map != null && map.isActiveAndEnabled;
        static readonly string[] Names = { "Собиратель", "Защитник", "Разведчик", "Исследователь", "Строитель", "Снабженец", "Медик", "Стабилизатор", "Безработный" };
        static readonly string[] Descriptions = { "Доставляет питание", "Подавляет угрозы", "Открывает территории", "Получает знания", "Доставляет материю", "Доставляет заряд", "Доставляет лекарства", "Защищает соседние узлы", "Ожидает назначения профессии" };
        static readonly string[] ResourceNames = { "Питание", "Знания", "Материя", "Заряд", "Лекарства", "Стабильность" };
        static readonly string[] States = { "Ожидает работу", "В пути", "Работает", "Восстанавливается", "Погиб", "На посту" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        { if (FindAnyObjectByType<ColonyView>() == null) new GameObject("Colony").AddComponent<ColonyView>(); }
        void Awake()
        { InitializeSession(); }
        void OnEnable()
        {
            if (Application.isPlaying && world == null) InitializeSession();
        }
        void InitializeSession()
        {
            // Runtime callbacks and the non-serialized model need rebuilding after a script reload.
            var previousInterface = generatedInterface != null ? generatedInterface : transform.Find("Interface")?.gameObject;
            var previousInput = generatedInput != null ? generatedInput : transform.Find("Input")?.gameObject;
            if (previousInterface != null) { previousInterface.SetActive(false); Destroy(previousInterface); }
            if (previousInput != null) { previousInput.SetActive(false); Destroy(previousInput); }
            researchButtons.Clear();
            researchPrices.Clear();
            if (disc != null) Destroy(disc);
            if (discTexture != null) Destroy(discTexture);
            layoutReady = false;
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateDisc(); BuildInterface(); Restart(sessionSeed);
        }
        void CreateDisc()
        {
            discTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                discTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)))));
            discTexture.Apply(); disc = Sprite.Create(discTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
        }
        void BuildInterface()
        {
            var canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            generatedInterface = canvasObject;
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler = scaler;
            scaler.referenceResolution = new Vector2(1000, 650); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                generatedInput = events;
                events.transform.SetParent(transform, false);
            }
            var background = Panel(canvasObject.transform, "Background", new Color(.025f, .04f, .06f)); Stretch(background);
            safeArea = Panel(background, "Safe area", Color.clear); Stretch(safeArea);
            var popupObject = new GameObject("Description popup", typeof(RectTransform), typeof(DescriptionPopup));
            popupObject.transform.SetParent(safeArea, false);
            descriptionPopup = popupObject.GetComponent<DescriptionPopup>(); descriptionPopup.Initialize(font);
            var viewport = new GameObject("Map viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(safeArea, false);
            mapViewport = (RectTransform)viewport.transform;
            Stretch(mapViewport); mapViewport.offsetMin = new Vector2(88, 140); mapViewport.offsetMax = new Vector2(0, -64);
            var mapObject = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(MapGraphic));
            mapObject.transform.SetParent(viewport.transform, false);
            map = mapObject.GetComponent<MapGraphic>(); map.Owner = this;
            Stretch((RectTransform)mapObject.transform);
            var top = Panel(safeArea, "Session", new Color(.06f, .085f, .11f)); Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), Vector2.zero);
            topPanel = top;
            playIcon = Button(top, "▶", 12, -7, 54, 50, () => { paused = !paused; Refresh(); }).GetComponentInChildren<UiIcon>();
            speedText = Button(top, "×1", 74, -7, 64, 50, () => { speedIndex = (speedIndex + 1) % Speeds.Length; accumulator = 0; Refresh(); }, holdText: "Скорость симуляции. MAX — максимально возможная в пределах времени кадра.").GetComponentInChildren<Text>();
            Button(top, "↻", 146, -7, 54, 50, () => Restart(world.Seed + 1));
            stocks = Label(top, "", 218, -9, 530, 45, 16);
            Button(top, "?", -118, -7, 48, 50, () => TogglePanel(help), true);
            Button(top, "…", -60, -7, 48, 50, () => TogglePanel(diagnostics), true);
            var bottom = Panel(safeArea, "Selection", new Color(.06f, .085f, .11f)); Anchor(bottom, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 140));
            selectionPanel = bottom;
            portrait = Panel(bottom, "Portrait", Color.white).GetComponent<Image>(); portrait.sprite = disc; Place(portrait.rectTransform, 70, -14, 56, 56);
            caption = Label(bottom, "", 8, -82, 180, 42, 17); caption.alignment = TextAnchor.UpperCenter;
            description = Label(bottom, "", 210, -14, 600, 28, 19);
            details = Label(bottom, "", 210, -44, 700, 30, 16);
            pawnActions = new GameObject("Profession actions", typeof(RectTransform)); pawnActions.transform.SetParent(bottom, false); Stretch((RectTransform)pawnActions.transform);
            Button(pawnActions.transform, "‹", 12, -18, 46, 48, () => ChangeRole(-1));
            Button(pawnActions.transform, "›", 138, -18, 46, 48, () => ChangeRole(1));
            convert = Button(pawnActions.transform, "Преобразовать", 210, -76, 180, 48, Convert);
            nodeActions = new GameObject("Territory actions", typeof(RectTransform)); nodeActions.transform.SetParent(bottom, false); Stretch((RectTransform)nodeActions.transform);
            accessButton = Button(nodeActions.transform, "Доступ", 210, -76, 180, 48, () => { world.ToggleAccess(SelectedNode); Refresh(); });
            Button(bottom, "⌂", -64, -14, 50, 48, () => { map.Pan = Vector2.zero; map.Zoom = Screen.height > Screen.width ? 40 : 60; SelectNode(World.Home); }, true);
            feedback = Label(bottom, "", 410, -88, 500, 30, 16);
            coreActions = new GameObject("Colony controls", typeof(RectTransform)); coreActions.transform.SetParent(safeArea, false);
            var colonyRect = (RectTransform)coreActions.transform;
            colonyControlsRect = colonyRect;
            colonyRect.anchorMin = colonyRect.anchorMax = new Vector2(0, 1); colonyRect.pivot = new Vector2(0, 1);
            colonyRect.anchoredPosition = new Vector2(12, -80); colonyRect.sizeDelta = new Vector2(64, 208);
            Button(coreActions.transform, "Найм", 0, 0, 64, 56, () => TogglePanel(recruitment), iconSymbol: "hire", holdText: "Найм пешек. Выберите профессию. Стоимость — 25 питания.");
            Button(coreActions.transform, "Исследования", 0, -72, 64, 56, () => TogglePanel(research), iconSymbol: "research", holdText: "Исследования колонии. Покупаются за знания. Удерживайте значок улучшения, чтобы прочитать его эффект.");
            Button(coreActions.transform, "Профессии", 0, -144, 64, 56, () => TogglePanel(workforce), iconSymbol: "pawn", holdText: "Численность профессий. +1 и +3 назначают безработных, −1 освобождает работника. Смена выполняется на ядре; счётчики учитывают назначенные переводы.");
            help = new GameObject("Help", typeof(RectTransform)); help.AddComponent<HelpPanel>().Initialize(safeArea, font, descriptionPopup); help.SetActive(false);
            diagnostics = Overlay("Diagnostics", ""); diagnosticText = diagnostics.transform.Find("Body").GetComponent<Text>();
            recruitment = Overlay("Recruitment", ""); recruitRoleText = recruitment.transform.Find("Body").GetComponent<Text>();
            recruitPortrait = Panel(recruitment.transform, "Portrait", Color.white).GetComponent<Image>(); recruitPortrait.sprite = disc; Place(recruitPortrait.rectTransform, 175, -115, 56, 56);
            Button(recruitment.transform, "‹", 90, -119, 48, 48, () => { recruitRole = (Profession)(((int)recruitRole + Names.Length - 1) % Names.Length); Refresh(); });
            Button(recruitment.transform, "›", 270, -119, 48, 48, () => { recruitRole = (Profession)(((int)recruitRole + 1) % Names.Length); Refresh(); });
            Label(recruitment.transform, World.RecruitCost + " питания", 20, -206, 370, 30, 19);
            recruitButton = Button(recruitment.transform, "Нанять", 20, -256, 370, 48, () => { world.Recruit(recruitRole); Refresh(); });
            research = Overlay("Research", "");
            foreach (var definition in ResearchProgress.Definitions)
            {
                var technology = definition;
                int index = researchButtons.Count;
                float x = 20 + index % 3 * 128;
                float y = -64 - index / 3 * 140;
                var button = Button(research.transform, technology.Name, x, y, 104, 64,
                    () => { world.ResearchTechnology(technology.Type); Refresh(); }, iconSymbol: technology.Icon,
                    holdDescription: () => technology.Name + "\nУровень " + world.Research.Level(technology.Type) + "/" + technology.MaxLevel + "\n" + technology.Description + "\n" + (world.Research.IsCompleted(technology.Type) ? "Максимальный уровень." : world.Research.NextCost(technology.Type) + " знаний за следующий уровень."));
                Label(research.transform, technology.Name, x, y - 68, 120, 24, 16);
                var price = Label(research.transform, "", x, y - 96, 120, 24, 16); price.color = Palette.Resources[(int)Resource.Knowledge]; researchPrices.Add(price);
                researchButtons.Add(button);
            }
            workforce = Overlay("Workforce", "");
            workforce.AddComponent<WorkforcePanel>().Initialize(font, () => world, Names);
        }
        GameObject Overlay(string name, string body)
        {
            var panel = Panel(safeArea, name, new Color(.08f, .115f, .15f));
            bool colonyPanel = name == "Recruitment" || name == "Research" || name == "Workforce";
            Vector2 anchor = colonyPanel ? new Vector2(0, 1) : new Vector2(1, 1);
            panel.anchorMin = panel.anchorMax = panel.pivot = anchor;
            panel.anchoredPosition = new Vector2(colonyPanel ? 88 : -12, -72); panel.sizeDelta = new Vector2(410, 390);
            var text = Label(panel, body, 20, -55, 370, 320, 17); text.name = "Body";
            Button(panel, "×", -56, -7, 48, 42, () => panel.gameObject.SetActive(false), true);
            panel.gameObject.SetActive(false); return panel.gameObject;
        }
        void TogglePanel(GameObject panel)
        {
            bool show = !panel.activeSelf;
            help.SetActive(false);
            diagnostics.SetActive(false);
            recruitment.SetActive(false);
            research.SetActive(false);
            workforce.SetActive(false);
            panel.SetActive(show);
            Refresh();
        }
        public void SelectPawn(int id) { SelectedPawn = id; chosenRole = world.Pawns[id].Role; Refresh(); }
        public void SelectNode(int id) { SelectedPawn = -1; SelectedNode = id; Refresh(); }
        void ChangeRole(int change) { chosenRole = (Profession)(((int)chosenRole + change + Names.Length) % Names.Length); Refresh(); }
        void Convert()
        {
            feedback.text = world.Convert(SelectedPawn, chosenRole) ? "Смена профессии назначена на ядре" : "Преобразование недоступно";
            Refresh();
        }
        void Restart(int seed)
        {
            sessionSeed = seed;
            world = new World(seed); map.World = world; accumulator = 0; paused = false;
            statistics.Reset();
            map.Pan = Vector2.zero; map.Zoom = Screen.height > Screen.width ? 40 : 60; help.SetActive(false); diagnostics.SetActive(false);
            recruitment.SetActive(false); research.SetActive(false);
            workforce.SetActive(false);
            feedback.text = ""; SelectNode(World.Home);
        }
        void Update()
        {
            Rect area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            ApplyMapLayout();
            int executedTicks = 0;
            if (!paused && !world.Lost)
            {
                bool maximum = Speeds[speedIndex] == 0;
                if (!maximum) accumulator = Mathf.Min(accumulator + Time.unscaledDeltaTime * Speeds[speedIndex], Speeds[speedIndex]);
                var budget = System.Diagnostics.Stopwatch.StartNew();
                while (!world.Lost && (maximum || accumulator >= .1f) && budget.Elapsed.TotalMilliseconds < 8)
                {
                    long before = budget.ElapsedTicks;
                    world.Step();
                    tickMs = (float)((budget.ElapsedTicks - before) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                    executedTicks++;
                    if (!maximum) accumulator -= .1f;
                }
            }
            statistics.Record(Time.unscaledDeltaTime, executedTicks);
            refresh += Time.unscaledDeltaTime;
            if (refresh > .2f) { refresh = 0; Refresh(); }
        }
        void Refresh()
        {
            if (world == null) return;
            ApplyMapLayout();
            playIcon.Symbol = paused ? "▶" : "Ⅱ"; playIcon.SetVerticesDirty();
            speedText.text = Speeds[speedIndex] == 0 ? "MAX" : "×" + Speeds[speedIndex];
            recruitRoleText.text = Names[(int)recruitRole] + "\n" + Descriptions[(int)recruitRole];
            recruitPortrait.color = Palette.Professions[(int)recruitRole];
            recruitButton.interactable = world.CanRecruit;
            for (int i = 0; i < researchButtons.Count; i++)
            {
                var technology = ResearchProgress.Definitions[i];
                bool completed = world.Research.IsCompleted(technology.Type);
                researchButtons[i].interactable = world.CanResearchTechnology(technology.Type);
                researchButtons[i].GetComponentInChildren<UiIcon>().color = completed ? Palette.Resources[2] : Color.white;
                researchPrices[i].text = world.Research.Level(technology.Type) + "/" + technology.MaxLevel + (completed ? " · MAX" : " · " + world.Research.NextCost(technology.Type) + " ◆");
            }
            stocks.text = "";
            for (int i = 0; i < 6; i++) stocks.text += "<color=#" + ColorUtility.ToHtmlStringRGB(Palette.Resources[i]) + ">■ " + world.Stores[(Resource)i].ToString("0") + "</color>  ";
            bool pawnSelected = SelectedPawn >= 0;
            pawnActions.SetActive(pawnSelected); nodeActions.SetActive(!pawnSelected && SelectedNode != World.Home && world.Nodes[SelectedNode].Known);
            if (pawnSelected)
            {
                var pawn = world.Pawns[SelectedPawn];
                portrait.color = Palette.Professions[(int)chosenRole]; caption.text = Names[(int)chosenRole]; description.text = Descriptions[(int)chosenRole];
                details.text = "";
                feedback.text = pawn.PendingRole.HasValue ? "На ядре → " + Names[(int)pawn.PendingRole.Value] : pawn.Health <= 0 ? "Погиб" : "";
                convert.interactable = pawn.Health > 0 && pawn.Role != chosenRole && pawn.PendingRole != chosenRole && !world.Lost;
            }
            else
            {
                var node = world.Nodes[SelectedNode]; portrait.color = node.Id == World.Home ? Color.white : Color.gray;
                caption.text = node.Id == World.Home ? "Ядро" : "Территория";
                description.text = node.Known ? "" : "Не исследована";
                details.text = "";
                if (node.Known) foreach (var deposit in node.Deposits) details.text += ResourceNames[(int)deposit.Type] + " " + deposit.Amount.ToString("0") + "  ";
                accessButton.GetComponentInChildren<Text>().text = node.Blocked ? "Открыть доступ" : "Закрыть доступ";
                feedback.text = "";
            }
            if (world.Lost) feedback.text = "Колония потеряна · ↻ начать заново";
            diagnosticText.text = "Seed " + world.Seed + "\nВремя " + world.Time.ToString("0") + " с\nТерритории " + world.Nodes.Count + "\nДороги " + world.Edges() + "\nПешки " + world.Pawns.Count + "\nТик " + tickMs.ToString("0.00") + " мс\nПоиски пути " + world.Searches + "\nМасштаб " + map.Zoom.ToString("0") + "\n" + map.InputStatus;
            diagnosticText.text = "FPS " + statistics.FramesPerSecond.ToString("0") + "   TPS " + statistics.TicksPerSecond.ToString("0") + "\n" + diagnosticText.text;
            if (pawnSelected)
            {
                var pawn = world.Pawns[SelectedPawn];
                diagnosticText.text += "\n" + Names[(int)pawn.Role] + " · " + States[(int)pawn.State] + "\nПитание " + pawn.Food.ToString("0") + " · силы " + pawn.Energy.ToString("0") + "\nЗдоровье " + pawn.Health.ToString("0");
            }
        }
        void OnDestroy() { if (disc) Destroy(disc); if (discTexture) Destroy(discTexture); }
        RectTransform Panel(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            obj.GetComponent<Image>().color = color; return (RectTransform)obj.transform;
        }
        Text Label(Transform parent, string value, float x, float y, float width, float height, int size)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = new Color(.87f, .92f, .96f); text.text = value;
            text.raycastTarget = false; text.supportRichText = true; Place(text.rectTransform, x, y, width, height); return text;
        }
        Button Button(Transform parent, string title, float x, float y, float width, float height, UnityEngine.Events.UnityAction action, bool right = false, string iconSymbol = null, string holdText = null, Func<string> holdDescription = null)
        {
            var panel = Panel(parent, title, new Color(.13f, .2f, .26f)); Place(panel, x, y, width, height);
            if (right) { panel.anchorMin = panel.anchorMax = Vector2.one; }
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            HoldDescription hold = null;
            if (iconSymbol != null || holdText != null || holdDescription != null || title.Length == 1)
            {
                hold = panel.gameObject.AddComponent<HoldDescription>();
                hold.Initialize(descriptionPopup, holdDescription ?? (() => holdText ?? IconDescription(iconSymbol ?? title, title)));
            }
            button.onClick.AddListener(() => { if (hold == null || !hold.SuppressClick) action(); });
            if (title.Length == 1 || iconSymbol != null)
            {
                var obj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiIcon)); obj.transform.SetParent(panel, false);
                var icon = obj.GetComponent<UiIcon>(); icon.Symbol = iconSymbol ?? title; icon.raycastTarget = false;
                Place(icon.rectTransform, (width - 28) / 2, -(height - 28) / 2, 28, 28);
            }
            else
            {
                var text = Label(panel, title, 0, 0, width, height, 21); text.alignment = TextAnchor.MiddleCenter;
            }
            return button;
        }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        static string IconDescription(string symbol, string fallback)
        {
            switch (symbol)
            {
                case "▶": return "Приостановить или продолжить симуляцию.";
                case "×": return "Закрыть окно.";
                case "↻": return "Начать заново на новой карте.";
                case "?": return "Обозначения и управление. Удерживайте значок для подробностей.";
                case "…": return "Статистика симуляции, TPS и FPS.";
                case "⌂": return "Вернуться к ядру колонии.";
                case "‹": return "Предыдущая профессия.";
                case "›": return "Следующая профессия.";
                default: return fallback;
            }
        }
        void ApplyMapLayout()
        {
            bool portraitMode = Screen.height > Screen.width;
            if (!layoutReady || portraitMode != portraitLayout)
            {
                portraitLayout = portraitMode;
                canvasScaler.referenceResolution = portraitMode ? new Vector2(480, 800) : new Vector2(1000, 650);
                map.Pan = Vector2.zero;
                map.Zoom = portraitMode ? 40 : 60;
                layoutReady = true;
            }
            float headerHeight = portraitMode ? 112 : 64;
            float bottomHeight = portraitMode ? 180 : 140;
            topPanel.offsetMin = new Vector2(0, -headerHeight);
            selectionPanel.offsetMax = new Vector2(0, bottomHeight);
            colonyControlsRect.anchoredPosition = new Vector2(12, -headerHeight - 16);
            Place(stocks.rectTransform, portraitMode ? 12 : 218, portraitMode ? -72 : -9, portraitMode ? safeArea.rect.width - 24 : 530, 36);
            Place(description.rectTransform, 210, -14, portraitMode ? Mathf.Max(100, safeArea.rect.width - 290) : 600, portraitMode ? 44 : 28);
            Place(details.rectTransform, 210, portraitMode ? -58 : -44, portraitMode ? safeArea.rect.width - 226 : 700, portraitMode ? 48 : 30);
            Place(convert.GetComponent<RectTransform>(), 210, portraitMode ? -118 : -76, 180, 48);
            Place(accessButton.GetComponent<RectTransform>(), 210, portraitMode ? -118 : -76, 180, 48);
            Place(feedback.rectTransform, portraitMode ? 12 : 410, portraitMode ? -136 : -88, portraitMode ? 184 : 500, portraitMode ? 36 : 30);
            PositionMenu(recruitment, portraitMode, true, bottomHeight);
            PositionMenu(research, portraitMode, true, bottomHeight);
            PositionMenu(workforce, portraitMode, true, bottomHeight);
            PositionMenu(diagnostics, portraitMode, false, bottomHeight);
            if (portraitMode)
            {
                bool menuOpen = recruitment.activeSelf || research.activeSelf || workforce.activeSelf || diagnostics.activeSelf || help.activeSelf;
                mapViewport.offsetMin = new Vector2(88, bottomHeight + (menuOpen ? 374 : 0));
                mapViewport.offsetMax = new Vector2(0, -headerHeight);
                return;
            }
            float left = 88;
            if (recruitment.activeSelf || research.activeSelf || workforce.activeSelf) left = 510;
            if (help.activeSelf) left = 100 + ((RectTransform)help.transform).rect.width;
            float right = diagnostics.activeSelf ? 434 : 0;
            mapViewport.offsetMin = new Vector2(left, bottomHeight);
            mapViewport.offsetMax = new Vector2(-right, -headerHeight);
        }
        static void PositionMenu(GameObject menu, bool portraitMode, bool left, float bottomHeight)
        {
            var rect = (RectTransform)menu.transform;
            Vector2 anchor = portraitMode ? new Vector2(.5f, 0) : new Vector2(left ? 0 : 1, 1);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = portraitMode ? new Vector2(0, bottomHeight + 12) : new Vector2(left ? 88 : -12, -72);
            rect.sizeDelta = new Vector2(410, portraitMode ? 350 : 390);
        }
    }
}
