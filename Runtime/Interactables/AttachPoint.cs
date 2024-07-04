using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace _3Dimensions.FishNet_XR.Runtime.Interactables
{
    public class AttachPoint : NetworkBehaviour
    {
        private static readonly List<AttachPoint> _GlobalAttachPoints = new List<AttachPoint>();
        public static List<AttachPoint> GlobalAttachPoints => _GlobalAttachPoints;
        
        public static List<ObjectLookingForAttachment> CloseObjects { get; private set; } = new List<ObjectLookingForAttachment>();

        [SerializeField] private GameObject visualWhenAttachable;
        public bool active;
        public List<AttachPointType> attachPointTypes;

        public bool logVisibility;

        public delegate void OnAttached();
        public event OnAttached OnAttachedEvent;
        
        public delegate void OnDetached();
        public event OnDetached OnDetachedEvent;

        private readonly SyncVar<NetworkBehaviour> _attachedObject = new SyncVar<NetworkBehaviour>();

        private void LateUpdate()
        {
            visualWhenAttachable.SetActive(Visible());
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _attachedObject.OnChange += AttachedObjectOnOnChange;
        }


        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _attachedObject.OnChange -= AttachedObjectOnOnChange;
        }

        private void OnEnable()
        {
            if (!_GlobalAttachPoints.Contains(this)) _GlobalAttachPoints.Add(this);
        }

        private void OnDisable()
        {
            if (_GlobalAttachPoints.Contains(this)) _GlobalAttachPoints.Remove(this);
        }

        private void AttachedObjectOnOnChange(NetworkBehaviour prev, NetworkBehaviour next, bool asserver)
        {
            if (next)
            {
                OnAttachedEvent?.Invoke();
            }
            else if (prev != null && next == null)
            {
                OnDetachedEvent?.Invoke();
            }
        } 
        
        public static AttachPoint ClosestAttachPoint(Vector3 position, List<AttachPointType> attachPointTypes, float maxDistance)
        {
            List<AttachPoint> filteredAttachPoints = new List<AttachPoint>();
            
            foreach (AttachPoint ap in GlobalAttachPoints)
            {
                foreach (AttachPointType type in attachPointTypes)
                {
                    if (ap.attachPointTypes.Contains(type) && ap.active)
                    {
                        if (!filteredAttachPoints.Contains(ap))
                        {
                            filteredAttachPoints.Add(ap);
                        }
                    }
                }
            }
            
            //Check if there are any AttachPoint that have the correct type
            if (filteredAttachPoints.Count == 0) return null;
            
            //Check if AttachPoint is within grabbing distance
            float shortestDistance = float.PositiveInfinity;
            AttachPoint closestAttachPoint = null;
            foreach (AttachPoint ap in filteredAttachPoints)
            {
                //Check for closest AttachPoint
                float distance = Vector3.Distance(ap.transform.position, position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    
                    //Check for attach distance
                    if (shortestDistance < maxDistance)
                    {
                        closestAttachPoint = ap;
                    }
                }
            }
            
            return closestAttachPoint;
        }

        public static void AddBehaviourToClosestList(NetworkBehaviour networkBehaviour, AttachPoint closestAttachPoint, float maxDistance)
        {
            ObjectLookingForAttachment existingObject =
                CloseObjects.Find(x => x.AttachableNetworkBehaviour == networkBehaviour);

            if (existingObject != null) CloseObjects.Remove(existingObject);
            
            ObjectLookingForAttachment newObjectLookingForAttachment = new()
            {
                AttachableNetworkBehaviour = networkBehaviour,
                ClosestAttachPoint = closestAttachPoint,
                MaxDistance = maxDistance
            };
                
            CloseObjects.Add(newObjectLookingForAttachment);
        }

        public static  void RemoveBehaviourFromClosestList(NetworkBehaviour networkBehaviour)
        {
            if (CloseObjects.Exists(x => x.AttachableNetworkBehaviour == networkBehaviour))
            {
                ObjectLookingForAttachment objectLookingForAttachmentToRemove =
                    CloseObjects.Find(x => x.AttachableNetworkBehaviour == networkBehaviour);
                CloseObjects.Remove(objectLookingForAttachmentToRemove);
            }
        }

        private bool Visible()
        {
            if (!active) return false;
            if (CloseObjects == null) return false;
            if (CloseObjects.Count == 0) return false;

            foreach (ObjectLookingForAttachment attachable in CloseObjects)
            {
                if (attachable.ClosestAttachPoint == this)
                {
                    if (Vector3.Distance(attachable.AttachableNetworkBehaviour.transform.position, transform.position) <
                        attachable.MaxDistance) return true;
                }
            }

            return false;
        }

        public class ObjectLookingForAttachment
        {
            public NetworkBehaviour AttachableNetworkBehaviour;
            public AttachPoint ClosestAttachPoint;
            public float MaxDistance;
        }
    }
}
