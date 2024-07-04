using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace _3Dimensions.FishNet_XR.Runtime.Interactables
{
    [ExecuteAlways]
    public class OffsetTester : MonoBehaviour
    {
        public Transform otherObject;
        public Vector3 offset;
        public Quaternion offsetRotation;
        public bool always;
        public bool drawGizmos;

        [Button]
        private void SetOffset()
        {
            offset = otherObject.transform.InverseTransformPoint(transform.position);
            offsetRotation = Quaternion.Inverse(transform.rotation) * otherObject.rotation;
        }

        [Button]
        private void SnapTopOffset()
        {
            transform.position = otherObject.position + otherObject.rotation * offset;
            transform.rotation = otherObject.rotation * Quaternion.Inverse(offsetRotation);
        }

        private void Update()
        {
            if (always)
            {
                SnapTopOffset();
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(otherObject.position, otherObject.position + otherObject.forward);
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(otherObject.position, otherObject.position + otherObject.rotation * offset);
        }
    }
}
