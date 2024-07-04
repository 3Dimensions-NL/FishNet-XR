using UnityEngine;
using UnityEngine.InputSystem;
namespace _3Dimensions.FishNet_XR.Runtime.Utilities
{
    public class TeleportControllerHandler : MonoBehaviour
    {
        public GameObject teleportVisuals;

        public InputActionReference teleportSelect;
        public InputActionReference teleportCancel;
        
        private bool _teleportAiming;
    
        void Start()
        {
            teleportSelect.action.started += TeleportStart;
            teleportSelect.action.canceled += TeleportCancel;
            teleportCancel.action.started += TeleportCancel;

            ApplyTeleportAimState(false);
        }

        private void OnDestroy()
        {
            teleportSelect.action.started -= TeleportStart;
            teleportSelect.action.canceled -= TeleportCancel;
            teleportCancel.action.started -= TeleportCancel;
        }

        private void LateUpdate()
        {
            ApplyTeleportAimState(_teleportAiming);
        }

        /// <summary>
        /// Receives input from the controller to initiate a start Teleporting. The player can now aim for a teleport location.
        /// </summary>
        /// <param name="context"></param>
        private void TeleportStart(InputAction.CallbackContext context)
        {
            // Debug.Log("Teleport Started,  started: " + context.started +", cancelled: " + context.canceled + ", performed: " + context.performed);
            _teleportAiming = true;
        }
        

        /// <summary>
        /// Receives input from the controller to cancel a Teleport.
        /// </summary>
        /// <param name="context"></param>
        private void TeleportCancel(InputAction.CallbackContext context)
        {
            // Debug.Log("Teleport Cancel,  started: " + context.started +", cancelled: " + context.canceled + ", performed: " + context.performed);
            _teleportAiming = false;
        }

        /// <summary>
        /// Apply a new Teleport Aim state
        /// </summary>
        /// <param name="state"></param>
        private void ApplyTeleportAimState(bool state)
        {
            _teleportAiming = state;
            teleportVisuals.SetActive(_teleportAiming);
        }
    }
}
