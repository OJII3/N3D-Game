using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

namespace Project
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float initialFOV = 40f;
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField]private PlayerInput playerInput;

        private const string ActionStringZoom = "Zoom";

        private void Awake()
        {
        }

        private void OnEnable()
        {
            playerInput.actions[ActionStringZoom].performed += OnZoom;
        }

        private void OnDestroy()
        {
            playerInput.actions[ActionStringZoom].performed -= OnZoom;
        }

        private void OnZoom(InputAction.CallbackContext ctx)
        {
            freeLookCamera.m_Lens.FieldOfView += ctx.ReadValue<float>();
        }
    }
}