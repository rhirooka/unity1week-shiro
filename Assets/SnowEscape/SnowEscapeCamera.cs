using UnityEngine;
using UnityEngine.InputSystem;

namespace SnowEscape
{
    /// <summary>
    /// 固定俯瞰視点とプレイヤー目線の一人称視点を切り替える。
    /// </summary>
    public sealed class SnowEscapeCamera : MonoBehaviour
    {
        private const float FirstPersonHeight = 1.45f;
        private const float FirstPersonForwardOffset = 0.12f;
        private const float FirstPersonFieldOfView = 75f;
        private const float MouseSensitivity = 0.12f;
        private const float MinimumPitch = -75f;
        private const float MaximumPitch = 75f;

        private Camera controlledCamera;
        private SnowEscapePlayer player;
        private Vector3 overviewPosition;
        private Quaternion overviewRotation;
        private float overviewOrthographicSize;
        private float overviewNearClipPlane;
        private bool gameplayActive;
        private float yaw;
        private float pitch;

        public bool IsFirstPerson { get; private set; }
        public Vector3 PlanarForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        public void Initialize(Camera targetCamera, SnowEscapePlayer targetPlayer)
        {
            controlledCamera = targetCamera;
            player = targetPlayer;
            overviewPosition = targetCamera.transform.position;
            overviewRotation = targetCamera.transform.rotation;
            overviewOrthographicSize = targetCamera.orthographicSize;
            overviewNearClipPlane = targetCamera.nearClipPlane;
            ResetView();
        }

        public void SetGameplayActive(bool active)
        {
            gameplayActive = active;
            if (!active) ResetView();
        }

        public void ResetView()
        {
            IsFirstPerson = false;
            yaw = 0f;
            pitch = 0f;
            if (player != null) player.SetFirstPersonView(false);
            SetCursorForFirstPerson(false);
            ApplyOverviewView();
        }

        public void TickInput()
        {
            if (controlledCamera == null || player == null) return;

            Keyboard keyboard = Keyboard.current;
            if (gameplayActive && keyboard != null && keyboard.cKey.wasPressedThisFrame)
            {
                IsFirstPerson = !IsFirstPerson;
                if (IsFirstPerson)
                {
                    Vector3 facing = player.Direction.sqrMagnitude > 0.001f
                        ? player.Direction.normalized
                        : Vector3.forward;
                    yaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
                    pitch = 0f;
                }
                player.SetFirstPersonView(IsFirstPerson);
                SetCursorForFirstPerson(IsFirstPerson);
                if (!IsFirstPerson) ApplyOverviewView();
            }

            if (!IsFirstPerson) return;

            // ダッシュを含むキーボード入力とは独立して、毎フレーム視点入力を読む。
            // UI側などがカーソル状態を変更した場合も、FPS中はすぐ中央固定へ戻す。
            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
                SetCursorForFirstPerson(true);

            UpdateFirstPersonLook();
        }

        private void UpdateFirstPersonLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseDelta = mouse.delta.ReadValue();
            yaw += mouseDelta.x * MouseSensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * MouseSensitivity, MinimumPitch, MaximumPitch);
        }

        private void LateUpdate()
        {
            if (controlledCamera == null || player == null) return;
            if (IsFirstPerson) ApplyFirstPersonView();
        }

        private void OnDisable()
        {
            SetCursorForFirstPerson(false);
        }

        private void ApplyOverviewView()
        {
            if (controlledCamera == null) return;
            controlledCamera.orthographic = true;
            controlledCamera.orthographicSize = overviewOrthographicSize;
            controlledCamera.nearClipPlane = overviewNearClipPlane;
            controlledCamera.transform.SetPositionAndRotation(overviewPosition, overviewRotation);
        }

        private void ApplyFirstPersonView()
        {
            Vector3 forward = PlanarForward;
            Vector3 position = player.Position + Vector3.up * FirstPersonHeight +
                               forward * FirstPersonForwardOffset;
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f);

            controlledCamera.orthographic = false;
            controlledCamera.fieldOfView = FirstPersonFieldOfView;
            controlledCamera.nearClipPlane = 0.05f;
            controlledCamera.transform.SetPositionAndRotation(
                position,
                viewRotation);
        }

        private static void SetCursorForFirstPerson(bool firstPerson)
        {
            Cursor.lockState = firstPerson ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !firstPerson;
        }
    }
}
