using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SnowEscape
{
    /// <summary>
    /// GAME_DESIGN.md の仕様を、空の SampleScene からも遊べる形で構築する。
    /// シーンへの手作業での参照設定を不要にするため、起動時に自動生成する。
    /// </summary>
    public sealed class SnowEscapeGame : MonoBehaviour
    {
        private const float WorldWidth = 36f;
        private const float WorldHeight = 20f;
        private const float CellSize = 0.5f;
        private const float NormalSpeed = 4f;
        private const float DashSpeed = 8.5f;
        private const float StaminaMax = 100f;
        private const float StaminaDrain = 35f;
        private const float StaminaRegen = 18f;
        private const float EnemyBaseSpeed = 2.6f;
        private const float EnemyRetargetSeconds = 0.5f;
        private const float EnemySpawnSeconds = 10f;
        private const float CollisionDistance = 0.78f;
        private const float InvincibleSeconds = 2f;
        private const float StrideLength = 1.3f;
        private const float CornerInset = 2.5f;

        private enum GameState { Title, Playing, Ended }

        private class Walker
        {
            public Vector3 Position;
            public Vector3 Direction = Vector3.forward;
            public float Stride;
            public bool LeftFoot;
            public float FootScale = 1f;
        }

        private sealed class Enemy : Walker
        {
            public GameObject Visual;
            public Vector3 Target;
            public float RetargetTimer;
            public float Speed;
            public bool Runner;
        }

        private static readonly Vector3[] SpawnCorners =
        {
            new(-WorldWidth / 2f + CornerInset, 0f, -WorldHeight / 2f + CornerInset),
            new( WorldWidth / 2f - CornerInset, 0f, -WorldHeight / 2f + CornerInset),
            new(-WorldWidth / 2f + CornerInset, 0f,  WorldHeight / 2f - CornerInset),
            new( WorldWidth / 2f - CornerInset, 0f,  WorldHeight / 2f - CornerInset)
        };

        private readonly List<Enemy> enemies = new();
        private readonly List<GameObject> footprints = new();
        private readonly List<GameObject> worldObjects = new();
        private readonly bool[,] packedSnow =
            new bool[Mathf.RoundToInt(WorldWidth / CellSize), Mathf.RoundToInt(WorldHeight / CellSize)];

        private Walker player;
        private GameObject playerVisual;
        private Transform playerBody;
        private Camera gameCamera;
        private Canvas canvas;
        private RectTransform titlePanel;
        private RectTransform resultPanel;
        private Text timerText;
        private Text livesText;
        private Text milestoneText;
        private Text finalTimeText;
        private Image staminaFill;
        private Image damageFlash;
        private GameState state;
        private float survivalTime;
        private float spawnTimer;
        private float stamina;
        private float invincibleTimer;
        private float flashTimer;
        private float milestoneTimer;
        private int lives;
        private int lastMilestone;
        private Material footprintMaterial;
        private Material ghostDebugMaterial;
        private Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SnowEscapeGame>() != null) return;
            new GameObject("Snow Escape Game").AddComponent<SnowEscapeGame>();
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildWorld();
            BuildInterface();
            ResetGame();
            ShowState(GameState.Title);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            UpdateEffects(dt);
            if (state != GameState.Playing) return;

            survivalTime += dt;
            spawnTimer += dt;
            invincibleTimer = Mathf.Max(0f, invincibleTimer - dt);

            UpdatePlayer(dt);
            SpawnEnemiesOverTime();
            UpdateEnemies(dt);
            UpdateHud();
            UpdateMilestones(dt);
        }

        private void BuildWorld()
        {
            foreach (Camera camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                camera.enabled = false;
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                light.enabled = false;
            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                listener.enabled = false;

            RenderSettings.ambientLight = new Color(0.58f, 0.65f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.10f, 0.14f, 0.19f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 48f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Snow Field";
            ground.transform.position = new Vector3(0f, -0.18f, 0f);
            ground.transform.localScale = new Vector3(WorldWidth, 0.3f, WorldHeight);
            ground.GetComponent<Renderer>().material = MakeMaterial(new Color(0.91f, 0.96f, 0.98f), 0.05f);
            worldObjects.Add(ground);

            var packedUnderlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            packedUnderlay.name = "Packed Snow Underlay";
            packedUnderlay.transform.position = new Vector3(0f, 0.005f, 0f);
            packedUnderlay.transform.localScale = new Vector3(WorldWidth - 0.2f, 0.025f, WorldHeight - 0.2f);
            packedUnderlay.GetComponent<Renderer>().material = MakeMaterial(new Color(0.75f, 0.84f, 0.88f), 0.02f);
            packedUnderlay.SetActive(false);
            worldObjects.Add(packedUnderlay);

            BuildTrees();
            BuildLightingAndCamera();
            BuildPlayer();

            footprintMaterial = MakeMaterial(new Color(0.34f, 0.43f, 0.48f), 0f);
            ghostDebugMaterial = MakeTransparentMaterial(new Color(0.55f, 0.82f, 1f, 0.24f));
        }

        private void BuildLightingAndCamera()
        {
            var sunObject = new GameObject("Winter Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.92f, 0.80f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            worldObjects.Add(sunObject);

            var cameraObject = new GameObject("Snow Escape Camera");
            gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 14f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.055f, 0.075f, 0.105f);
            gameCamera.transform.position = new Vector3(0f, 30f, -12.1f);
            gameCamera.transform.LookAt(Vector3.zero);
            cameraObject.AddComponent<AudioListener>();
            worldObjects.Add(cameraObject);
        }

        private void BuildTrees()
        {
            for (float x = -WorldWidth / 2f; x <= WorldWidth / 2f; x += 3.6f)
            {
                MakeTree(new Vector3(x, 0f, -WorldHeight / 2f - 0.7f), 0.85f + Random.value * 0.3f);
                MakeTree(new Vector3(x + 1.2f, 0f, WorldHeight / 2f + 0.7f), 0.85f + Random.value * 0.3f);
            }
            for (float z = -WorldHeight / 2f + 2f; z < WorldHeight / 2f; z += 3.5f)
            {
                MakeTree(new Vector3(-WorldWidth / 2f - 0.7f, 0f, z), 0.85f + Random.value * 0.3f);
                MakeTree(new Vector3(WorldWidth / 2f + 0.7f, 0f, z + 1.1f), 0.85f + Random.value * 0.3f);
            }
        }

        private void MakeTree(Vector3 position, float scale)
        {
            var root = new GameObject("Snowy Pine");
            root.transform.position = position;
            root.transform.localScale = Vector3.one * scale;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            trunk.transform.localScale = new Vector3(0.22f, 0.45f, 0.22f);
            trunk.GetComponent<Renderer>().material = MakeMaterial(new Color(0.24f, 0.15f, 0.10f), 0f);

            for (int i = 0; i < 3; i++)
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                crown.transform.SetParent(root.transform, false);
                crown.transform.localPosition = new Vector3(0f, 0.82f + i * 0.43f, 0f);
                float width = 0.95f - i * 0.18f;
                crown.transform.localScale = new Vector3(width, 0.28f, width);
                crown.GetComponent<Renderer>().material =
                    MakeMaterial(i == 2 ? new Color(0.22f, 0.39f, 0.31f) : new Color(0.12f, 0.29f, 0.23f), 0f);
            }
            worldObjects.Add(root);
        }

        private void BuildPlayer()
        {
            player = new Walker();
            playerVisual = new GameObject("Player");

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Coat";
            body.transform.SetParent(playerVisual.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            body.transform.localScale = new Vector3(0.52f, 0.62f, 0.52f);
            body.GetComponent<Renderer>().material = MakeMaterial(new Color(0.82f, 0.20f, 0.16f), 0.05f);
            playerBody = body.transform;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(playerVisual.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.40f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            head.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.77f, 0.60f), 0.02f);

            var hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hat.name = "White Hat";
            hat.transform.SetParent(playerVisual.transform, false);
            hat.transform.localPosition = new Vector3(0f, 1.63f, -0.02f);
            hat.transform.localScale = new Vector3(0.62f, 0.28f, 0.62f);
            hat.GetComponent<Renderer>().material = MakeMaterial(Color.white, 0f);
            worldObjects.Add(playerVisual);
        }

        private void BuildInterface()
        {
            var canvasObject = new GameObject("Snow Escape UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                worldObjects.Add(eventSystem);
            }

            timerText = CreateText("Timer", canvas.transform, "00:00", 42, TextAnchor.UpperLeft);
            SetRect(timerText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(35f, -28f), new Vector2(250f, 70f));
            livesText = CreateText("Lives", canvas.transform, "♥ ♥ ♥", 34, TextAnchor.UpperRight);
            livesText.color = new Color(1f, 0.45f, 0.40f);
            SetRect(livesText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-35f, -30f), new Vector2(260f, 60f));

            var hint = CreateText("Controls", canvas.transform,
                "移動: WASD / 矢印キー　　ダッシュ: Shift", 21, TextAnchor.LowerLeft);
            hint.color = new Color(0.86f, 0.91f, 0.94f);
            SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(28f, 24f), new Vector2(520f, 48f));

            var staminaBack = CreateImage("Stamina Background", canvas.transform, new Color(0f, 0f, 0f, 0.42f));
            SetRect(staminaBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 35f), new Vector2(360f, 18f));
            staminaFill = CreateImage("Stamina", staminaBack.transform, new Color(0.23f, 0.78f, 0.62f));
            staminaFill.type = Image.Type.Filled;
            staminaFill.fillMethod = Image.FillMethod.Horizontal;
            staminaFill.fillOrigin = 0;
            Stretch(staminaFill.rectTransform, 3f);

            milestoneText = CreateText("Milestone", canvas.transform, "", 70, TextAnchor.MiddleCenter);
            milestoneText.color = new Color(1f, 1f, 1f, 0f);
            Stretch(milestoneText.rectTransform, 0f);

            damageFlash = CreateImage("Damage Flash", canvas.transform, new Color(1f, 0.08f, 0.04f, 0f));
            Stretch(damageFlash.rectTransform, 0f);
            damageFlash.raycastTarget = false;

            titlePanel = CreatePanel("Title Screen", new Color(0.035f, 0.05f, 0.075f, 0.98f));
            CreateCenteredTitle(titlePanel, "Snow Escape", "見えない鬼から、雪に残る足跡を頼りに逃げ続けろ。", "スタート", StartGame);

            resultPanel = CreatePanel("Result Screen", new Color(0.025f, 0.04f, 0.06f, 0.90f));
            var title = CreateText("Caught", resultPanel, "捕まりました", 66, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 175f), new Vector2(700f, 90f));
            finalTimeText = CreateText("Final Time", resultPanel, "生存時間 00:00", 38, TextAnchor.MiddleCenter);
            SetRect(finalTimeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(700f, 70f));
            CreateButton(resultPanel, "もう一度", new Vector2(-145f, -75f), StartGame, false);
            CreateButton(resultPanel, "タイトルへ戻る", new Vector2(145f, -75f), ReturnToTitle, true);
        }

        private void CreateCenteredTitle(RectTransform panel, string heading, string description, string button, Action action)
        {
            var title = CreateText("Title", panel, heading, 78, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 190f), new Vector2(900f, 110f));
            var subtitle = CreateText("Subtitle", panel, description, 30, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.82f, 0.88f, 0.92f);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(1000f, 90f));
            CreateButton(panel, button, new Vector2(0f, -85f), action, false);
        }

        private void StartGame()
        {
            ResetGame();
            ShowState(GameState.Playing);
        }

        private void ReturnToTitle()
        {
            ResetGame();
            ShowState(GameState.Title);
        }

        private void ResetGame()
        {
            foreach (Enemy enemy in enemies)
                if (enemy.Visual != null) Destroy(enemy.Visual);
            enemies.Clear();
            foreach (GameObject mark in footprints)
                if (mark != null) Destroy(mark);
            footprints.Clear();
            Array.Clear(packedSnow, 0, packedSnow.Length);

            player.Position = Vector3.zero;
            player.Direction = Vector3.forward;
            player.Stride = 0f;
            player.LeftFoot = false;
            playerVisual.transform.position = player.Position;
            playerVisual.transform.rotation = Quaternion.identity;
            playerVisual.SetActive(true);

            survivalTime = 0f;
            spawnTimer = 0f;
            stamina = StaminaMax;
            invincibleTimer = 0f;
            lives = 3;
            lastMilestone = 0;
            milestoneTimer = 0f;
            for (int i = 0; i < SpawnCorners.Length; i++)
                SpawnEnemy(SpawnCorners[i], 1f);
            UpdateHud();
        }

        private void ShowState(GameState next)
        {
            state = next;
            titlePanel.gameObject.SetActive(next == GameState.Title);
            resultPanel.gameObject.SetActive(next == GameState.Ended);
            bool playing = next == GameState.Playing;
            timerText.gameObject.SetActive(playing);
            livesText.gameObject.SetActive(playing);
            staminaFill.transform.parent.gameObject.SetActive(playing);
            playerVisual.SetActive(next != GameState.Title);
        }

        private void EndGame()
        {
            finalTimeText.text = $"生存時間  {FormatTime(survivalTime)}";
            ShowState(GameState.Ended);
        }

        private void UpdatePlayer(float dt)
        {
            Vector2 input = ReadMovement();
            bool moving = input.sqrMagnitude > 0.001f;
            bool dashHeld = Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            bool dashing = moving && dashHeld && stamina > 0.01f;
            stamina = Mathf.Clamp(stamina + (dashing ? -StaminaDrain : StaminaRegen) * dt, 0f, StaminaMax);

            if (moving)
            {
                input.Normalize();
                Vector3 direction = new Vector3(input.x, 0f, input.y);
                float distance = (dashing ? DashSpeed : NormalSpeed) * dt;
                Vector3 next = player.Position + direction * distance;
                next.x = Mathf.Clamp(next.x, -WorldWidth / 2f + 0.55f, WorldWidth / 2f - 0.55f);
                next.z = Mathf.Clamp(next.z, -WorldHeight / 2f + 0.55f, WorldHeight / 2f - 0.55f);
                float actualDistance = Vector3.Distance(player.Position, next);
                player.Position = next;
                player.Direction = direction;
                playerVisual.transform.position = next;
                playerVisual.transform.rotation = Quaternion.LookRotation(direction);
                float bounce = Mathf.Sin(Time.time * (dashing ? 15f : 10f)) * 0.045f;
                playerBody.localPosition = new Vector3(0f, 0.62f + bounce, 0f);
                AdvanceFootsteps(player, actualDistance, dashing ? 1.55f : 1f);
            }
        }

        private void SpawnEnemiesOverTime()
        {
            while (spawnTimer >= EnemySpawnSeconds)
            {
                spawnTimer -= EnemySpawnSeconds;
                Vector3 corner = SpawnCorners[Random.Range(0, SpawnCorners.Length)];
                SpawnEnemy(corner, Random.value < 1f / 3f ? 3f : 1f);
            }
        }

        private void SpawnEnemy(Vector3 position, float footScale)
        {
            position += new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Invisible Oni (debug silhouette)";
            visual.transform.position = position + Vector3.up * 0.75f;
            visual.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            visual.GetComponent<Renderer>().material = ghostDebugMaterial;
            visual.SetActive(false);

            float variation = Random.Range(0.88f, 1.12f);
            enemies.Add(new Enemy
            {
                Position = position,
                Target = player.Position,
                Speed = EnemyBaseSpeed * variation,
                RetargetTimer = Random.Range(0.05f, EnemyRetargetSeconds),
                FootScale = footScale,
                Visual = visual,
                Runner = Random.value < 0.18f
            });
        }

        private void UpdateEnemies(float dt)
        {
            foreach (Enemy enemy in enemies)
            {
                enemy.RetargetTimer -= dt;
                if (enemy.RetargetTimer <= 0f)
                {
                    enemy.Target = player.Position;
                    enemy.RetargetTimer += EnemyRetargetSeconds + Random.Range(-0.08f, 0.08f);
                }

                Vector3 toTarget = enemy.Target - enemy.Position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.01f)
                {
                    Vector3 direction = toTarget.normalized;
                    float runnerBoost = enemy.Runner && Mathf.Repeat(survivalTime + enemy.Speed, 9f) < 1.4f ? 1.75f : 1f;
                    float distance = Mathf.Min(toTarget.magnitude, enemy.Speed * runnerBoost * dt);
                    enemy.Position += direction * distance;
                    enemy.Direction = direction;
                    AdvanceFootsteps(enemy, distance, enemy.FootScale);
                }
                enemy.Visual.transform.position = enemy.Position + Vector3.up * 0.75f;

                if (invincibleTimer <= 0f &&
                    Vector3.Distance(enemy.Position, player.Position) <= CollisionDistance)
                {
                    lives--;
                    invincibleTimer = InvincibleSeconds;
                    flashTimer = 0.42f;
                    if (lives <= 0)
                    {
                        EndGame();
                        return;
                    }
                }
            }
        }

        private void AdvanceFootsteps(Walker walker, float distance, float scale)
        {
            walker.Stride += distance;
            while (walker.Stride >= StrideLength)
            {
                walker.Stride -= StrideLength;
                PlaceFootprint(walker.Position, walker.Direction, walker.LeftFoot, scale);
                walker.LeftFoot = !walker.LeftFoot;
            }
        }

        private void PlaceFootprint(Vector3 position, Vector3 direction, bool left, float scale)
        {
            int gx = Mathf.FloorToInt((position.x + WorldWidth / 2f) / CellSize);
            int gy = Mathf.FloorToInt((position.z + WorldHeight / 2f) / CellSize);
            if (gx < 0 || gy < 0 || gx >= packedSnow.GetLength(0) || gy >= packedSnow.GetLength(1)) return;
            packedSnow[gx, gy] = true;

            Vector3 lateral = Vector3.Cross(Vector3.up, direction).normalized * (left ? 0.27f : -0.27f);
            var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "Permanent Footprint";
            mark.transform.position = new Vector3(position.x, 0.025f, position.z) + lateral;
            mark.transform.rotation = Quaternion.LookRotation(direction);
            mark.transform.localScale = new Vector3(0.32f * scale, 0.025f, 0.64f * scale);
            mark.GetComponent<Renderer>().material = footprintMaterial;
            Collider collider = mark.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            footprints.Add(mark);
        }

        private void UpdateHud()
        {
            timerText.text = FormatTime(survivalTime);
            livesText.text = lives > 0 ? string.Join(" ", new string('♥', lives).ToCharArray()) : "";
            staminaFill.fillAmount = stamina / StaminaMax;
            staminaFill.color = stamina < 22f
                ? new Color(1f, 0.44f, 0.28f)
                : new Color(0.23f, 0.78f, 0.62f);
            float pulse = invincibleTimer > 0f ? Mathf.PingPong(Time.time * 7f, 1f) : 1f;
            foreach (Renderer renderer in playerVisual.GetComponentsInChildren<Renderer>())
                renderer.enabled = invincibleTimer <= 0f || pulse > 0.35f;
        }

        private void UpdateMilestones(float dt)
        {
            int minute = Mathf.FloorToInt(survivalTime / 60f);
            if (minute > lastMilestone)
            {
                lastMilestone = minute;
                milestoneTimer = 2.2f;
                milestoneText.text = $"{minute} 分 生存！";
            }
            if (milestoneTimer > 0f)
            {
                milestoneTimer -= dt;
                float alpha = Mathf.Clamp01(Mathf.Min(milestoneTimer * 2f, (2.2f - milestoneTimer) * 2f));
                milestoneText.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void UpdateEffects(float dt)
        {
            if (flashTimer <= 0f) return;
            flashTimer -= dt;
            float alpha = Mathf.Clamp01(flashTimer / 0.42f) * 0.34f;
            damageFlash.color = new Color(1f, 0.06f, 0.02f, alpha);
        }

        private static Vector2 ReadMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
            return new Vector2(x, y);
        }

        private static string FormatTime(float value)
        {
            int total = Mathf.FloorToInt(value);
            return $"{total / 60:00}:{total % 60:00}";
        }

        private RectTransform CreatePanel(string name, Color color)
        {
            Image image = CreateImage(name, canvas.transform, color);
            Stretch(image.rectTransform, 0f);
            return image.rectTransform;
        }

        private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private void CreateButton(Transform parent, string label, Vector2 position, Action action, bool secondary)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(260f, 72f));
            var image = go.GetComponent<Image>();
            image.color = secondary ? new Color(0.28f, 0.34f, 0.39f, 0.95f) : new Color(0.08f, 0.56f, 0.39f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            var text = CreateText("Label", go.transform, label, 27, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 0f);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Material MakeMaterial(Color color, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            material.color = color;
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_Metallic", metallic);
            return material;
        }

        private static Material MakeTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var material = new Material(shader);
            material.color = color;
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.renderQueue = 3000;
            return material;
        }
    }
}
