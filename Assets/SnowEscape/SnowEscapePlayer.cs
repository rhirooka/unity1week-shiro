using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowEscape
{
    /// <summary>
    /// プレイヤー固有の入力、移動、スタミナ、無敵表示と見た目を管理する。
    /// </summary>
    public sealed class SnowEscapePlayer : MonoBehaviour
    {
        public readonly struct Movement
        {
            public Movement(float distance, float footprintScale)
            {
                Distance = distance;
                FootprintScale = footprintScale;
            }

            public float Distance { get; }
            public float FootprintScale { get; }
        }

        private const float WorldWidth = 36f;
        private const float WorldHeight = 20f;
        private const float NormalSpeed = 4f;
        private const float DashSpeed = 8.5f;
        private const float StaminaMaximum = 100f;
        private const float StaminaDrain = 35f;
        private const float StaminaRegen = 18f;
        private const float InvincibleSeconds = 2f;
        private const float StrideLength = 1.3f;

        private Renderer[] renderers;
        private Transform body;
        private Vector3 bodyBasePosition;
        private float stride;
        private bool leftFoot;
        private float invincibleTimer;

        public Vector3 Position => transform.position;
        public Vector3 Direction { get; private set; } = Vector3.forward;
        public float Stamina { get; private set; } = StaminaMaximum;
        public float StaminaRatio => Stamina / StaminaMaximum;
        public bool CanBeHit => invincibleTimer <= 0f;

        public static SnowEscapePlayer CreateOrReuse()
        {
            GameObject visual = FindAuthoredPenguin();
            if (visual == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/PenguinPlayer");
                visual = prefab != null ? Instantiate(prefab) : BuildFallbackVisual();
            }

            visual.name = "PenguinPlayer";
            SnowEscapePlayer controller = visual.GetComponent<SnowEscapePlayer>();
            if (controller == null) controller = visual.AddComponent<SnowEscapePlayer>();
            controller.CacheVisualParts();
            return controller;
        }

        private static GameObject FindAuthoredPenguin()
        {
            GameObject authoredPenguin = null;
            GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject candidate in sceneObjects)
            {
                if (candidate.name != "PenguinPlayer" || !candidate.scene.IsValid()) continue;

                bool hasPenguinParts = candidate.transform.Find("Belly") != null &&
                                       candidate.transform.Find("Beak") != null &&
                                       candidate.transform.Find("LeftWing") != null &&
                                       candidate.transform.Find("RightWing") != null;
                if (hasPenguinParts)
                {
                    authoredPenguin = candidate;
                    break;
                }
            }

            if (authoredPenguin == null) return null;

            // With scene reload disabled, a player generated during an earlier
            // play session can remain. Hide it so only the authored model is used.
            foreach (GameObject candidate in sceneObjects)
            {
                if (candidate != authoredPenguin && candidate.name == "PenguinPlayer" && candidate.scene.IsValid())
                    candidate.SetActive(false);
            }

            authoredPenguin.SetActive(true);
            return authoredPenguin;
        }

        public void ResetPlayer()
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Direction = Vector3.forward;
            Stamina = StaminaMaximum;
            stride = 0f;
            leftFoot = false;
            invincibleTimer = 0f;
            SetRenderersVisible(true);
            if (body != null) body.localPosition = bodyBasePosition;
            gameObject.SetActive(true);
        }

        public void TickTimers(float deltaTime)
        {
            invincibleTimer = Mathf.Max(0f, invincibleTimer - deltaTime);
            bool visible = invincibleTimer <= 0f || Mathf.PingPong(Time.time * 7f, 1f) > 0.35f;
            SetRenderersVisible(visible);
        }

        public Movement Move(float deltaTime)
        {
            Vector2 input = ReadMovement();
            bool moving = input.sqrMagnitude > 0.001f;
            Keyboard keyboard = Keyboard.current;
            bool dashHeld = keyboard != null && keyboard.spaceKey.isPressed;
            bool dashing = moving && dashHeld && Stamina > 0.01f;
            Stamina = Mathf.Clamp(
                Stamina + (dashing ? -StaminaDrain : StaminaRegen) * deltaTime,
                0f,
                StaminaMaximum);

            if (!moving) return new Movement(0f, 1f);

            input.Normalize();
            Vector3 direction = new(input.x, 0f, input.y);
            float requestedDistance = (dashing ? DashSpeed : NormalSpeed) * deltaTime;
            Vector3 oldPosition = transform.position;
            Vector3 next = oldPosition + direction * requestedDistance;
            next.x = Mathf.Clamp(next.x, -WorldWidth / 2f + 0.55f, WorldWidth / 2f - 0.55f);
            next.z = Mathf.Clamp(next.z, -WorldHeight / 2f + 0.55f, WorldHeight / 2f - 0.55f);

            Direction = direction;
            transform.SetPositionAndRotation(next, Quaternion.LookRotation(direction));
            if (body != null)
            {
                float bounce = Mathf.Sin(Time.time * (dashing ? 15f : 10f)) * 0.045f;
                body.localPosition = bodyBasePosition + Vector3.up * bounce;
            }

            return new Movement(Vector3.Distance(oldPosition, next), dashing ? 1.55f : 1f);
        }

        public bool TryTakeFootstep(float movedDistance, out bool useLeftFoot)
        {
            stride += movedDistance;
            if (stride < StrideLength)
            {
                useLeftFoot = false;
                return false;
            }

            stride -= StrideLength;
            useLeftFoot = leftFoot;
            leftFoot = !leftFoot;
            return true;
        }

        public void TakeHit()
        {
            invincibleTimer = InvincibleSeconds;
        }

        public void SetPresentationActive(bool active)
        {
            gameObject.SetActive(active);
        }

        private void CacheVisualParts()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            body = transform.Find("Body");
            if (body != null) bodyBasePosition = body.localPosition;
        }

        private void SetRenderersVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (Renderer playerRenderer in renderers)
                if (playerRenderer != null) playerRenderer.enabled = visible;
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

        private static GameObject BuildFallbackVisual()
        {
            var root = new GameObject("PenguinPlayer");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            body.transform.localScale = new Vector3(0.52f, 0.62f, 0.52f);
            body.GetComponent<Renderer>().material = MakeMaterial(new Color(0.82f, 0.20f, 0.16f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.40f, 0f);
            head.transform.localScale = Vector3.one * 0.55f;
            head.GetComponent<Renderer>().material = MakeMaterial(new Color(1f, 0.77f, 0.60f));

            var hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hat.name = "White Hat";
            hat.transform.SetParent(root.transform, false);
            hat.transform.localPosition = new Vector3(0f, 1.63f, -0.02f);
            hat.transform.localScale = new Vector3(0.62f, 0.28f, 0.62f);
            hat.GetComponent<Renderer>().material = MakeMaterial(Color.white);
            return root;
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Smoothness", 0.18f);
            return material;
        }
    }
}
