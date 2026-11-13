using UnityEngine;
using System.Collections.Generic;

namespace PawnStories
{
    public sealed class StoryView : MonoBehaviour
    {
        StorySession session;
        string seedText = "4271";
        int selected;
        int tab;
        int expandedEvent = -1;
        bool showReason;
        bool addingTicket;
        string ticketTitle = "";
        Motive ticketMotive;
        Vector2 rosterScroll, sheetScroll, boardScroll;
        GUIStyle heading, wrapped, button;
        string error;
        readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();
        GUIContent[] tabs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            new GameObject("Pawn Stories").AddComponent<StoryView>();
        }

        void Awake() => session = new StorySession(4271);

        void OnGUI()
        {
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, wordWrap = true };
                wrapped = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { wordWrap = true, padding = new RectOffset(6, 6, 5, 5) };
                tabs = new[] { Label("Обзор", "target"), Label("Характер", "user-round"),
                    Label("История пешки", "book-open"), Label("Общий журнал", "list") };
            }

            float scale = Mathf.Min(1f, Screen.width / 840f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            GUILayout.BeginArea(new Rect(12, 12, width - 24, height - 24), GUI.skin.box);
            DrawToolbar();
            GUILayout.BeginHorizontal();
            DrawRoster();
            if (tab == 3) DrawJournal(null);
            else DrawProfile(session.Pawns[selected]);
            DrawBoard(Mathf.Min(315, width * .35f));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawToolbar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Пешки колонии", heading, GUILayout.ExpandWidth(true));
            GUILayout.Label("Seed", GUILayout.Width(35));
            seedText = GUILayout.TextField(seedText, 11, GUILayout.Width(105));
            if (GUILayout.Button(Label("Новая группа", "refresh-cw"), button, GUILayout.Width(130)))
            {
                if (int.TryParse(seedText, out int seed))
                {
                    session = new StorySession(seed);
                    selected = 0;
                    expandedEvent = -1;
                    showReason = false;
                    rosterScroll = sheetScroll = boardScroll = Vector2.zero;
                    error = null;
                }
                else error = "Введите целое число от −2147483648 до 2147483647.";
            }
            if (GUILayout.Button(Label("Следующий час", "step-forward"), button, GUILayout.Width(145))) session.Step();
            GUILayout.EndHorizontal();
            GUILayout.Label(Label($"День {session.Day} · {session.Hour:00}:00  /  Seed {session.Seed}", "clock"), wrapped);
            if (error != null) GUILayout.Label(error, wrapped);
            int nextTab = GUILayout.Toolbar(tab, tabs, button);
            if (nextTab != tab)
            {
                tab = nextTab;
                sheetScroll = Vector2.zero;
                expandedEvent = -1;
            }
            GUILayout.Space(8);
        }

        void DrawRoster()
        {
            rosterScroll = GUILayout.BeginScrollView(rosterScroll, GUI.skin.box, GUILayout.Width(135));
            GUILayout.Label("Группа", heading);
            for (int i = 0; i < session.Pawns.Count; i++)
            {
                PawnProfile profile = session.Pawns[i].Profile;
                DailyPhase phase = session.Pawns[i].Schedule.At(session.Hour);
                var item = Label(profile.Name, PhaseIcon(phase), PhaseLabel(phase));
                if (GUILayout.Toggle(selected == i, item, button) && selected != i)
                {
                    selected = i;
                    sheetScroll = Vector2.zero;
                    expandedEvent = -1;
                    showReason = false;
                }
            }
            GUILayout.EndScrollView();
        }

        void DrawProfile(Pawn pawn)
        {
            if (tab == 2)
            {
                DrawJournal(pawn.Profile);
                return;
            }
            sheetScroll = GUILayout.BeginScrollView(sheetScroll, GUI.skin.box, GUILayout.ExpandWidth(true));
            PawnProfile profile = pawn.Profile;
            GUILayout.Label($"{profile.Name}, {profile.Age} лет", heading);
            GUILayout.Label(Label($"{profile.Origin} · {profile.Background}", Icon(profile.Value)), wrapped);
            GUILayout.Space(8);
            if (tab == 0) DrawOverview(pawn);
            else
            {
                Meter("Общительность", profile.Sociability, icon: "heart");
                Meter("Смелость", profile.Boldness, icon: "compass");
                Meter("Упорство", profile.Persistence, icon: "target");
                GUILayout.Label(Label(StoryText.MotiveName(profile.Value), Icon(profile.Value)), wrapped);
                GUILayout.Label($"+  {StoryText.TopicName(profile.Interest)}     −  {StoryText.TopicName(profile.Dislike)}", wrapped);
                GUILayout.Space(8);
                GUILayout.Label("Навыки", heading);
                for (int i = 0; i < profile.Skills.Count; i++)
                    Meter(StoryText.SkillName((Skill)i), profile.Skills[i], 10, Icon((Motive)i));
            }
            GUILayout.EndScrollView();
        }

        void DrawOverview(Pawn pawn)
        {
            DailySchedule schedule = pawn.Schedule;
            DailyPhase phase = schedule.At(session.Hour);
            GUILayout.Label(Label(PhaseLabel(phase), PhaseIcon(phase)), wrapped);
            GUILayout.Label(Label($"Сон {schedule.SleepHour:00}–{schedule.WakeHour:00} · Работа {schedule.WorkHour:00}–{schedule.WorkEnd:00}", "clock"), wrapped);
            Decision decision = pawn.Intention;
            if (decision == null) GUILayout.Label("Ожидает первого решения", wrapped);
            else
            {
                bool hasGoal = decision.Kind == DecisionKind.Personal || decision.Kind == DecisionKind.Ticket;
                GUILayout.Label(Label(StoryText.Intention(decision), hasGoal ? Icon(decision.Goal) : "clock"), heading);
                if (hasGoal) GUILayout.Label(StoryText.Goal(decision.Goal), wrapped);
                if (GUILayout.Button(Label("Почему?", "circle-help"), button, GUILayout.Width(106))) showReason = !showReason;
                if (showReason) GUILayout.Label(decision.Reason, wrapped);
            }
            GUILayout.Space(8);
            GUILayout.Label("Потребности", heading);
            Meter("Энергия", pawn.Energy, icon: "zap");
            Meter("Голод", pawn.Hunger, icon: "heart");
            GUILayout.Label("Желания", heading);
            for (int i = 0; i < pawn.Desires.Count; i++)
                Meter(StoryText.MotiveName((Motive)i), pawn.Desires[i], icon: Icon((Motive)i));
        }

        void DrawJournal(PawnProfile profile)
        {
            sheetScroll = GUILayout.BeginScrollView(sheetScroll, GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Label(profile == null ? "События колонии" : $"История · {profile.Name}", heading);
            if (profile != null) GUILayout.Label(Label(profile.Memory, "book-open"), wrapped);
            int currentTurn = -1;
            int count = 0;
            for (int i = session.Events.Count - 1; i >= 0 && count < 100; i--)
            {
                StoryEvent entry = session.Events[i];
                if (profile != null && entry.PawnId != profile.Id) continue;
                count++;
                if (entry.Turn != currentTurn)
                {
                    currentTurn = entry.Turn;
                    int elapsed = 8 + currentTurn;
                    GUILayout.Label(Label($"День {elapsed / 24 + 1} · {elapsed % 24:00}:00", "clock"), heading);
                }
                string actor = profile == null ? entry.Actor + " · " : "";
                string symbol = entry.PawnId.HasValue ? "chevron-right" : "clipboard-list";
                if (GUILayout.Button(Label(actor + entry.Action, symbol), button))
                    expandedEvent = expandedEvent == i ? -1 : i;
                if (expandedEvent == i && entry.Reason != null)
                    GUILayout.Label(entry.Reason, wrapped);
            }
            if (count == 0) GUILayout.Label("Событий пока нет", wrapped);
            GUILayout.EndScrollView();
        }

        void DrawBoard(float width)
        {
            boardScroll = GUILayout.BeginScrollView(boardScroll, GUI.skin.box, GUILayout.Width(width));
            GUILayout.Label(Label("Доска задач", "clipboard-list"), heading);
            session.SetTicketsEnabled(GUILayout.Toggle(session.TicketsEnabled, "Задачи видны пешкам"));
            if (GUILayout.Button(addingTicket ? "Отмена" : "+  Новая задача", button))
                addingTicket = !addingTicket;
            if (addingTicket)
            {
                GUILayout.Label("Название задачи", wrapped);
                ticketTitle = GUILayout.TextField(ticketTitle, 80);
                GUILayout.BeginHorizontal();
                foreach (Motive motive in System.Enum.GetValues(typeof(Motive)))
                {
                    if (GUILayout.Toggle(ticketMotive == motive,
                        Label("", Icon(motive), StoryText.MotiveName(motive)), button))
                        ticketMotive = motive;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(StoryText.MotiveName(ticketMotive), wrapped);
                using (new EditorlessDisabledScope(string.IsNullOrWhiteSpace(ticketTitle)))
                {
                    if (GUILayout.Button("Опубликовать", button) && session.Publish(ticketTitle, ticketMotive))
                    {
                        ticketTitle = "";
                        addingTicket = false;
                    }
                }
            }
            for (int i = 0; i < session.Tickets.Count; i++)
            {
                WorkTicket ticket = session.Tickets[i];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(Label(ticket.Title, Icon(ticket.Motive)), wrapped);
                int done = ticket.Total - ticket.Remaining;
                Meter(ticket.Remaining == 0 ? "Готово" : ticket.Priority == 0 ? "Скрыта" :
                    $"Приоритет {ticket.Priority}", done, ticket.Total,
                    ticket.Remaining == 0 ? "check" : null);
                using (new EditorlessDisabledScope(ticket.Remaining == 0 || !session.TicketsEnabled))
                {
                    GUILayout.BeginHorizontal();
                    for (int priority = 0; priority <= 3; priority++)
                    {
                        string caption = priority == 0 ? "" : priority.ToString();
                        string tooltip = priority == 0 ? "Снять задачу" : $"Приоритет {priority}";
                        if (GUILayout.Toggle(ticket.Priority == priority,
                            Label(caption, priority == 0 ? "x" : null, tooltip), button))
                            session.SetPriority(i, priority);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        static string Icon(Motive motive)
        {
            switch (motive)
            {
                case Motive.Connection: return "heart";
                case Motive.Security: return "shield";
                case Motive.Discovery: return "compass";
                default: return "wrench";
            }
        }

        static string PhaseIcon(DailyPhase phase) =>
            phase == DailyPhase.Sleep ? "clock" : phase == DailyPhase.Work ? "clipboard-list" : "user-round";

        static string PhaseLabel(DailyPhase phase) =>
            phase == DailyPhase.Sleep ? "Сон" : phase == DailyPhase.Work ? "Рабочее время" : "Личное время";

        GUIContent Label(string label, string icon, string tooltip = null)
        {
            if (icon == null) return new GUIContent(label, tooltip);
            if (!icons.TryGetValue(icon, out Texture2D image))
            {
                image = Resources.Load<Texture2D>($"Lucide/{icon}");
                icons.Add(icon, image);
            }
            return new GUIContent(label, image, tooltip);
        }

        void Meter(string label, int value, int maximum = 100, string icon = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Label(label, icon), wrapped, GUILayout.Width(140));
            Rect meter = GUILayoutUtility.GetRect(45, 12, GUILayout.ExpandWidth(true));
            meter.y += 3;
            meter.height = 9;
            Color previous = GUI.color;
            GUI.color = new Color(.25f, .27f, .31f);
            GUI.DrawTexture(meter, Texture2D.whiteTexture);
            meter.width *= (float)value / maximum;
            GUI.color = new Color(.35f, .74f, .64f);
            GUI.DrawTexture(meter, Texture2D.whiteTexture);
            GUI.color = previous;
            GUILayout.Label(value.ToString(), wrapped, GUILayout.Width(30));
            GUILayout.EndHorizontal();
        }

        readonly struct EditorlessDisabledScope : System.IDisposable
        {
            readonly bool enabled;
            public EditorlessDisabledScope(bool disabled)
            {
                enabled = GUI.enabled;
                GUI.enabled = enabled && !disabled;
            }
            public void Dispose() => GUI.enabled = enabled;
        }
    }
}
