using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
namespace _3Dimensions.FishNet_XR.Runtime.Player
{
    public class XROriginReferences : MonoBehaviour
    {
        public static XROriginReferences Instance => _instance;
        private static XROriginReferences _instance;

        [Header("Main VR Components")]
        public Transform head;
        public Transform handLeft;
        public Transform handRight;

        [Header("Laser Pointer information")] 
        [SerializeField] private LineRenderer leftLaserLineRenderer;
        [SerializeField] private LineRenderer rightLaserLineRenderer;
        [SerializeField] private GameObject laserPointerReticleLeft;
        [SerializeField] private GameObject laserPointerReticleRight;
        
        [Header("Teleport Pointer information")] 
        [SerializeField] private LineRenderer leftTeleportLineRenderer;
        [SerializeField] private LineRenderer rightTeleportLineRenderer;
        [SerializeField] private GameObject teleportHandlerLeft;
        [SerializeField] private GameObject teleportHandlerRight;
        [SerializeField] private GameObject teleportPointerLeft;
        [SerializeField] private GameObject teleportPointerRight;
        [SerializeField] private GameObject teleportPointerLeftReticle;
        [SerializeField] private GameObject teleportPointerRightReticle;

        [Header("Locomotion")]
        public XROrigin xrOrigin;
        public LocomotionProvider locomotionProvider;
        public TeleportationProvider teleportationProvider;

        public Vector3[] LeftLaserPointerPositions()
        {
            Vector3[] positions = new Vector3[2];
            positions[0] = handLeft.position;
            positions[1] = leftLaserLineRenderer.GetPosition(leftLaserLineRenderer.positionCount - 1);
            return positions;
        }
        public Vector3[] RightLaserPointerPositions()
        {
            Vector3[] positions = new Vector3[2];
            positions[0] = handRight.position;
            positions[1] = rightLaserLineRenderer.GetPosition(rightLaserLineRenderer.positionCount - 1);
            return positions;
        }

        public Vector3[] LeftTeleportPoints()
        {
            Vector3[] points = new Vector3[leftTeleportLineRenderer.positionCount];
            leftTeleportLineRenderer.GetPositions(points);
            return points;
        }

        public Vector3[] RightTeleportPoints()
        {
            Vector3[] points = new Vector3[rightTeleportLineRenderer.positionCount];
            rightTeleportLineRenderer.GetPositions(points);
            return points;
        }

        public Quaternion LeftReticleRotation => laserPointerReticleLeft.transform.rotation;
        public Quaternion RightReticleRotation => laserPointerReticleRight.transform.rotation;
        public Quaternion LeftTeleportReticleRotation => teleportPointerLeftReticle.transform.rotation;
        public Quaternion RightTeleportReticleRotation => teleportPointerRightReticle.transform.rotation;
        public bool LaserPointerLeftOn => laserPointerReticleLeft.activeSelf;
        public bool LaserPointerRightOn => laserPointerReticleRight.activeSelf;
        public bool TeleportPointerLeftOn => teleportPointerLeft.activeSelf;
        public bool TeleportPointerRightOn => teleportPointerRight.activeSelf;
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            
            if (_instance != this)
            {
                DestroyImmediate(gameObject);
            }

            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        }
        
        public void SetTeleportationActive(bool newActive)
        {
            teleportHandlerLeft.SetActive(newActive);
            teleportHandlerRight.SetActive(newActive);
        }
    }
    
    public enum Hand
    {
        Left,
        Right
    }
}
