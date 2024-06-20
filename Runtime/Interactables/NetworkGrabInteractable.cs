using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace _3Dimensions.FishNet_XR.Runtime.Interactables
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class NetworkGrabInteractable : NetworkBehaviour
    {
        private XRSimpleInteractable _simpleInteractable;

        private void Awake()
        {
            _simpleInteractable = GetComponent<XRSimpleInteractable>();
        }
    }
}
