using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project
{
    public class CameraController : MonoBehaviour
    {
        private const string ActionStringZoom = "Zoom";
        [SerializeField] private float initialFOV = 40f;
        [SerializeField] private CinemachineFreeLook freeLookCamera;
        [SerializeField] private PlayerInput playerInput;

        private void Awake()
        {
        }

        private void OnEnable()
        {
            playerInput.actions[ActionStringZoom].performed += OnZoom;
            freeLookCamera.m_Lens.FieldOfView = initialFOV;
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