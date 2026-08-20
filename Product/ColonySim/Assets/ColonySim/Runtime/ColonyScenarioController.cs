using System.Collections.Generic;
using System.Linq;
using ColonySim.Domain;
using UnityEngine;

namespace ColonySim.Runtime
{
    public sealed class ColonyScenarioController : MonoBehaviour
    {
        [SerializeField] private HealthModelSelection healthModel = HealthModelSelection.Functional;
        [SerializeField] private bool autoRun = true;
        [SerializeField, Min(0.05f)] private float secondsPerStep = 0.45f;
        [SerializeField] private int seed = 20260821;

        private readonly Dictionary<string, SpriteRenderer> pawnViews = new Dictionary<string, SpriteRenderer>();
        private readonly Dictionary<string, SpriteRenderer> taskViews = new Dictionary<string, SpriteRenderer>();
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private ColonyScenario scenario;
        private Texture2D pixelTexture;
        private Sprite squareSprite;
        private float elapsed;
        private Vector2 eventScroll;

        public ColonySimulation Simulation => scenario?.Simulation;
        public HealthModelSelection HealthModel => healthModel;
        public int PawnViewCount => pawnViews.Count;
        public int TaskViewCount => taskViews.Count;

        private void Awake()
        {
            RebuildScenario(healthModel, autoRun);
        }

        private void Update()
        {
            if (!autoRun || Simulation == null)
            {
                return;
            }

            elapsed += Time.deltaTime;
            while (elapsed >= secondsPerStep)
            {
                elapsed -= secondsPerStep;
                StepOnce();
            }
        }

        private void OnDestroy()
        {
            DestroyVisuals();
            DestroyAsset(squareSprite);
            DestroyAsset(pixelTexture);
        }

        public void RebuildScenario(HealthModelSelection model, bool shouldAutoRun)
        {
            healthModel = model;
            autoRun = shouldAutoRun;
            elapsed = 0f;
            eventScroll = Vector2.zero;
            scenario = ColonyScenarioFactory.Create(model, seed);
            BuildVisuals();
            RefreshVisuals();
        }

        public void StepOnce()
        {
            Simulation?.Step();
            RefreshVisuals();
        }

        private void BuildVisuals()
        {
            DestroyVisuals();
            EnsureSprite();

            foreach (var obstacle in scenario.Obstacles)
            {
                CreateSquare($"Obstacle {obstacle}", obstacle, new Color(0.18f, 0.2f, 0.24f), 0);
            }

            foreach (var task in Simulation.Tasks)
            {
                taskViews[task.Id] = CreateSquare(
                    $"Task {task.DisplayName}",
                    task.Target,
                    new Color(0.95f, 0.62f, 0.15f),
                    1);
            }

            var pawnColors = new[]
            {
                new Color(0.25f, 0.8f, 1f),
                new Color(0.55f, 1f, 0.45f),
                new Color(0.95f, 0.4f, 0.75f)
            };

            for (var index = 0; index < Simulation.Pawns.Count; index++)
            {
                var pawn = Simulation.Pawns[index];
                pawnViews[pawn.Id] = CreateSquare(
                    $"Pawn {pawn.DisplayName}",
                    pawn.Position,
                    pawnColors[index % pawnColors.Length],
                    2);
            }
        }

        private SpriteRenderer CreateSquare(
            string objectName,
            GridPosition position,
            Color color,
            int sortingOrder)
        {
            var view = new GameObject(objectName);
            view.transform.SetParent(transform, false);
            view.transform.position = ToWorld(position);
            view.transform.localScale = Vector3.one * 0.68f;
            var renderer = view.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            spawnedObjects.Add(view);
            return renderer;
        }

        private void RefreshVisuals()
        {
            if (Simulation == null)
            {
                return;
            }

            foreach (var pawn in Simulation.Pawns)
            {
                if (pawnViews.TryGetValue(pawn.Id, out var view))
                {
                    view.transform.position = ToWorld(pawn.Position);
                }
            }

            foreach (var task in Simulation.Tasks)
            {
                if (taskViews.TryGetValue(task.Id, out var view))
                {
                    view.color = task.Completed
                        ? new Color(0.25f, 0.72f, 0.35f)
                        : task.AssignedPawnId == null
                            ? new Color(0.95f, 0.62f, 0.15f)
                            : new Color(1f, 0.9f, 0.25f);
                }
            }
        }

        private void EnsureSprite()
        {
            if (squareSprite != null)
            {
                return;
            }

            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "ColonySim Runtime Pixel",
                filterMode = FilterMode.Point
            };
            pixelTexture.SetPixel(0, 0, Color.white);
            pixelTexture.Apply();
            squareSprite = Sprite.Create(pixelTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            squareSprite.name = "ColonySim Runtime Square";
        }

        private void DestroyVisuals()
        {
            foreach (var spawnedObject in spawnedObjects.Where(item => item != null))
            {
                DestroyAsset(spawnedObject);
            }

            spawnedObjects.Clear();
            pawnViews.Clear();
            taskViews.Clear();
        }

        private static void DestroyAsset(Object asset)
        {
            if (asset == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(asset);
            }
            else
            {
                DestroyImmediate(asset);
            }
        }

        private static Vector3 ToWorld(GridPosition position)
        {
            return new Vector3(
                position.X - (ColonyScenarioFactory.Width - 1) * 0.5f,
                position.Y - (ColonyScenarioFactory.Height - 1) * 0.5f,
                0f);
        }

        private void OnGUI()
        {
            if (Simulation == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12, 12, 480, Screen.height - 24), GUI.skin.box);
            GUILayout.Label("ColonySim — архитектурная проба");
            GUILayout.Label($"Шаг: {Simulation.StepNumber}   Модель: {ModelTitle(healthModel)}");

            var selected = GUILayout.SelectionGrid(
                (int)healthModel,
                new[] { "Простое состояние", "Функциональное состояние" },
                2);
            if (selected != (int)healthModel)
            {
                RebuildScenario((HealthModelSelection)selected, autoRun);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(autoRun ? "Пауза" : "Запуск"))
            {
                autoRun = !autoRun;
            }

            if (GUILayout.Button("Один шаг"))
            {
                StepOnce();
            }

            if (GUILayout.Button("Сброс"))
            {
                RebuildScenario(healthModel, autoRun);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Пешки");
            foreach (var pawn in Simulation.Pawns)
            {
                GUILayout.Label($"• {pawn.DisplayName} {pawn.Position}: {pawn.Status}");
            }

            GUILayout.Space(6);
            GUILayout.Label("Задания");
            foreach (var task in Simulation.Tasks)
            {
                GUILayout.Label($"• {task.DisplayName}: {task.Status}");
            }

            GUILayout.Space(6);
            GUILayout.Label("Последние решения");
            eventScroll = GUILayout.BeginScrollView(eventScroll, GUILayout.Height(110));
            foreach (var entry in Simulation.Events.Skip(Mathf.Max(0, Simulation.Events.Count - 8)))
            {
                GUILayout.Label(entry);
            }
            GUILayout.EndScrollView();
            GUILayout.Label("Оранжевый — доступная работа; жёлтый — назначенная; зелёный — завершённая; серый — препятствие.");
            GUILayout.EndArea();
        }

        private static string ModelTitle(HealthModelSelection model)
        {
            return model == HealthModelSelection.Simple ? "простая" : "функциональная";
        }
    }
}
