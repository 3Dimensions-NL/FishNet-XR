using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace _3Dimensions.FishNet_XR.Runtime.Interactables
{
    [RequireComponent(typeof(XRSimpleInteractable), typeof(Rigidbody))]
    public class NetworkGrabInteractable : NetworkBehaviour
    {
        [SerializeField] private List<AttachPointType> attachPointTypes = new List<AttachPointType>();
        [SerializeField] private AttachPoint startAttachPoint;
        [SerializeField] private bool snapBackToStartAttachPoint;
        [SerializeField] private float attachDistance = 0.1f;
        [SerializeField] private float resetDistance = 5f;
        [SerializeField] private float velocityFactor = 10;
        [SerializeField] private bool snapToInteractor;
        
        [ShowInInspector] public AttachPoint AttachedPoint => _attachedPoint.Value;
        [ShowInInspector] private NetworkConnection CurrentOwner => Owner;

        [ShowInInspector] public State CurrentState => _state.Value;
        
        private readonly SyncVar<State> _state = new SyncVar<State>();
        private readonly SyncVar<AttachPoint> _attachedPoint = new SyncVar<AttachPoint>();
        
        public bool Grabbed => _state.Value == State.PickedUp;

        public UnityEvent onActivated;
        public UnityEvent onDeactivated;
        
        [ServerRpc(RunLocally = true, RequireOwnership = false)] public void SetState(State value) => _state.Value = value;
        [ServerRpc(RunLocally = true, RequireOwnership = false)] public void SetAttachPoint(AttachPoint newAttachPoint) => _attachedPoint.Value = newAttachPoint;

        private Vector3 _resetPosition;
        private Vector3 _lastPosition;
        private Vector3 _lastVelocity;
        private Vector3 _lastAngularVelocity;
        private Quaternion _lastRotation;
        private XRSimpleInteractable _simpleInteractable;
        private Rigidbody _rigidBody;
        private IXRInteractor _interactor;
        private bool _releasedLastTick;
        private Vector3 _grabOffset;
        private Quaternion _grabRotationOffset;

        public enum State
        {
            Loose,
            Attached,
            PickedUp,
        }
        
        private bool OutOfBounds
        {
            get
            {
                float distance = Vector3.Distance(transform.position, _resetPosition);
                return distance > resetDistance;
            }
        }
        
        private void Awake()
        {
            _simpleInteractable = GetComponent<XRSimpleInteractable>();
            _rigidBody = GetComponent<Rigidbody>();
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _resetPosition = transform.position;

            if (startAttachPoint)
            {
                AttachToAttachPoint(startAttachPoint, true);
            }
            
            HandleState();
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            _simpleInteractable.selectEntered.AddListener(OnSelectEntered);
            _simpleInteractable.selectExited.AddListener(OnSelectExited);

            _simpleInteractable.activated.AddListener(OnActivated);
            _simpleInteractable.deactivated.AddListener(OnDeactivated);
            
            HandleState();
        }



        public override void OnStopClient()
        {
            base.OnStopClient();
            
            _simpleInteractable.selectEntered.RemoveListener(OnSelectEntered);
            _simpleInteractable.selectExited.RemoveListener(OnSelectExited);
            
            _simpleInteractable.activated.RemoveListener(OnActivated);
            _simpleInteractable.deactivated.RemoveListener(OnDeactivated);

            if (IsOwner)
            {
                SetState(State.Loose);
                ServerRemoveOwnership();
            }
            
            HandleState();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnTick += OnTick;
            
            _state.OnChange += StateOnOnChange;
            _attachedPoint.OnChange += AttachPointChanged;
            
            HandleState();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnTick -= OnTick;

            _state.OnChange -= StateOnOnChange;
            _attachedPoint.OnChange -= AttachPointChanged;
            
            SetState(State.Loose);
            
            HandleState();
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            if (Owner != ClientManager.Connection)
            {
                //We lost our ownership
                Debug.Log("Ownership change to someone else!", this);
                Release();
            }
        }
        
        private void OnSelectEntered(SelectEnterEventArgs arg0)
        {
            Debug.Log("On Select Entered " + arg0.interactorObject, gameObject);
            _interactor = arg0.interactorObject;
            Grab();
        }
        
        private void OnSelectExited(SelectExitEventArgs arg0)
        {
            Debug.Log("On Select Exited " + arg0.interactorObject, gameObject);
            Release();
        }

        private void OnActivated(ActivateEventArgs arg0)
        {
            Debug.Log("OnActivated: " + arg0.interactableObject, gameObject);
            if (_interactor == null) return;
            if (_interactor != arg0.interactorObject) return;
            Activated();
        }

        private void OnDeactivated(DeactivateEventArgs arg0)
        {
            Debug.Log("OnDeactivated: " + arg0.interactableObject, gameObject);
            if (_interactor == null) return;
            if (_interactor != arg0.interactorObject) return;
            Deactivated();
        }

        private void Grab()
        {
            if (_interactor == null) return;
            
            ServerGiveOwnership(ClientManager.Connection);

            if (!snapToInteractor)
            {
                _grabOffset = _interactor.transform.InverseTransformPoint(transform.position);
                _grabRotationOffset = Quaternion.Inverse(transform.rotation) * _interactor.transform.rotation;
                
                Debug.Log("Grab offsets are: " + _grabOffset + ", " + _grabRotationOffset);
            }
            
            SetState(State.PickedUp);
            SetAttachPoint(null);
            HandleState();
        }

        private void Release()
        {
            _interactor = null;
            _releasedLastTick = true;

            if (snapBackToStartAttachPoint)
            {
                AttachToAttachPoint(startAttachPoint, true);
                return;
            }
            
            AttachPoint possibleAttachPoint = AttachPoint.ClosestAttachPoint(transform.position, attachPointTypes, attachDistance);
            if (possibleAttachPoint)
            {
                Debug.Log("Found a AttachPoint...");
                SetAttachPoint(possibleAttachPoint);
                return;
            }
            
            
            SetState(State.Loose);
            HandleState();
        }
        
        private void StateOnOnChange(State prev, State next, bool asServer)
        {
            HandleState();
        }
        
        private void AttachPointChanged(AttachPoint prev, AttachPoint next, bool asServer)
        {
            Debug.Log("AttachPointOnChange");
            if (next)
            {
                if (asServer)
                {
                    SetState(State.Attached);
                    RemoveOwnership();
                }
                transform.SetPositionAndRotation(next.transform.position, next.transform.rotation);
            }
            
            HandleState();
        }
        
        private void OnTick()
        {
            HandleState();
            
            //Check AttachPoints for availability
            if (AttachPoint.GlobalAttachPoints != null)
            {
                if (_state.Value == State.PickedUp)
                {
                    //Show close Attachable Points
                    AttachPoint closestAttachPoint = AttachPoint.ClosestAttachPoint(transform.position, attachPointTypes, attachDistance);
                    if (closestAttachPoint)
                    {
                        AttachPoint.AddBehaviourToClosestList(this, closestAttachPoint, attachDistance);
                    }
                    else
                    {
                        AttachPoint.RemoveBehaviourFromClosestList(this);
                    }
                }
                else
                {
                    AttachPoint.RemoveBehaviourFromClosestList(this);
                }
            }
            
            //Handle out of bounds only on the server
            if (IsServerStarted && OutOfBounds)
            {
                if (OutOfBounds)
                {
                    transform.position = _resetPosition;
                    if (_rigidBody)
                    {
                        _rigidBody.velocity = Vector3.zero;
                        _rigidBody.angularVelocity = Vector3.zero;
                    }
                }
            }
        }
        
        private void Update()
        {
            //Match position with interactor
            if (_interactor != null)
            {
                if (snapToInteractor)
                {
                    transform.position = _interactor.transform.position;
                    transform.rotation = _interactor.transform.rotation;
                }
                else
                {
                    Vector3 calculatedPosition = _interactor.transform.position + _interactor.transform.rotation * _grabOffset;
                    transform.position = calculatedPosition;
                    transform.rotation = _interactor.transform.rotation * Quaternion.Inverse(_grabRotationOffset);
                }
                
                //This client is holding the object, overwriting all physics
                _rigidBody.isKinematic = true;
            }
        }

        private void FixedUpdate()
        {
            //Collect velocity data
            _lastVelocity = (transform.position - _lastPosition) * velocityFactor;
            _lastAngularVelocity = _lastRotation.eulerAngles - transform.rotation.eulerAngles;
                
            //Reset last position;
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        public void AttachToAttachPoint(AttachPoint attachPointToAttach, bool forceAttach)
        {
            if (attachPointToAttach == null) return;
            
            if (IsOwner || forceAttach)
            {
                SetAttachStateOnServer(true, attachPointToAttach);
            }
        }

        public void DetachFromAttachPoint(AttachPoint attachPointToDetach, bool forceDetach)
        {
            if (IsOwner || forceDetach)
            {
                //Detached even when attachPointToDetach is null
                SetAttachStateOnServer(false, attachPointToDetach);
            }
        }

        [ServerRpc(RequireOwnership = false, RunLocally = true)]
        public void SetAttachStateOnServer(bool newAttachState, AttachPoint attachPoint)
        {
            // Debug.Log("SetAttachStateOnServer, "+ newAttachState + " position = " + position + ", rotation = " + rotation, gameObject);
            if (newAttachState)
            {
                _attachedPoint.Value = attachPoint;
                transform.SetPositionAndRotation(AttachedPoint.transform.position, AttachedPoint.transform.rotation);
            }
            else
            {
                _attachedPoint.Value = null;
            }
            
            _resetPosition = transform.position;

            HandleState();
        }

        private void HandleState()
        {
            if (AttachedPoint)
            {
                //Force kinematic when attached
                _rigidBody.isKinematic = true;
                transform.SetPositionAndRotation(AttachedPoint.transform.position, AttachedPoint.transform.rotation);
                return;
            }
            
            if (_interactor != null)
            {
                //Force kinematic physics while holding
                _rigidBody.isKinematic = true;
                return;
            }
            
            if (IsOwner)
            {
                //Force non-kinematic physics while is owner
                _rigidBody.isKinematic = false;
                if (_releasedLastTick)
                {
                    Debug.LogWarning("Released Last Frame... Applying velocity vectors");
                    _rigidBody.velocity = _lastVelocity;
                    _rigidBody.angularVelocity = _lastAngularVelocity;
                    
                    _releasedLastTick = false;
                }

                return;
            }
            
            switch (CurrentState)
            {
                case State.Loose:
                    _rigidBody.isKinematic = !HasAuthority;
                    break;
                case State.PickedUp:
                    _rigidBody.isKinematic = true;
                    _resetPosition = transform.position;
                    break;
                case State.Attached:
                    _rigidBody.isKinematic = true;
                    _resetPosition = transform.position;
                    break;
            }
        }

        [Button]
        private void AttachToSetAttachPoint()
        {
            if (startAttachPoint)
            {
                transform.SetPositionAndRotation(startAttachPoint.transform.position, startAttachPoint.transform.rotation);
            }
        }
        
        [ServerRpc (RequireOwnership = false, RunLocally = true)]
        private void ServerGiveOwnership(NetworkConnection connection)
        {
            Debug.Log("Giving ownership to: " + connection.ClientId, this);
            GiveOwnership(connection);
            HandleState();
        }

        [ServerRpc (RequireOwnership = true, RunLocally = true)]
        private void ServerRemoveOwnership()
        {
            Debug.Log("Removing ownership", this);
            RemoveOwnership();
            HandleState();
        }

        [ServerRpc]
        private void Activated()
        {
            onActivated.Invoke();
            ActivatedObserver();
        }

        [ObserversRpc]
        private void ActivatedObserver()
        {
            if (IsServerStarted) return;
            onActivated.Invoke();
        }        

        [ServerRpc]
        private void Deactivated()
        {
            onDeactivated.Invoke();
            DeactivatedObserver();
        }
        
        [ObserversRpc]
        private void DeactivatedObserver()
        {
            if (IsServerStarted) return;
            onDeactivated.Invoke();
        }      
    }
}
