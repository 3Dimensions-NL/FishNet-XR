using System;
using System.Collections;
using System.Collections.Generic;
using _3Dimensions.FishNet_XR.Runtime.Player;
using _3Dimensions.FishNet_XR.Runtime.Utilities;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _3Dimensions.XR.CameraRig
{
    public class XRPlayer : NetworkBehaviour
    {
        public static XRPlayer LocalInstance;
        public static readonly List<XRPlayer> Players = new();
        public static Action<XRPlayer> OnPlayerAdded;
        public static Action<XRPlayer> OnPlayerRemoved;

        [Header("External Client Transforms")]
        public Transform clientHeadTransform;
        public Transform clientLeftHandTransform;
        public Transform clientRightHandTransform;

        [Header("Laser Pointers")]
        [SerializeField] private GameObject leftLaserPointer;
        [SerializeField] private GameObject rightLaserPointer;
        
        [Header("Teleport Lines")]
        [SerializeField] private GameObject leftTeleportPointer;
        [SerializeField] private GameObject rightTeleportPointer;
        private bool LeftTeleportPointerOn => _syncedLeftTeleportPointerOn.Value;
        private bool RightTeleportPointerOn => _syncedRightTeleportPointerOn.Value;

        private readonly SyncVar<bool> _syncedLeftTeleportPointerOn = new SyncVar<bool>();
        private readonly SyncVar<bool> _syncedRightTeleportPointerOn = new SyncVar<bool>();

        private void SetLeftTeleportPointerOn(bool value) => _syncedLeftTeleportPointerOn.Value = value;
        private void SetRightTeleportPointerOn(bool value) => _syncedRightTeleportPointerOn.Value = value;

        [Header("Disconnect")]
        private Vector3 _lastHeadPosition;
        public bool autoDisconnect;
        public float disconnectTime = 15f;
        private float _disconnectTimer;

        [BoxGroup("Transport"), ShowInInspector] public bool Transporting => _syncedTransporting.Value;
        private readonly SyncVar<bool> _syncedTransporting = new SyncVar<bool>();
        [BoxGroup("Transport"), ShowInInspector] public bool ReadyForTransport => _syncedReadyForTransport.Value;
        private readonly SyncVar<bool> _syncedReadyForTransport = new SyncVar<bool>();
        
        [BoxGroup("Transport"), ShowInInspector] private float _transportSequenceDuration = 1f;
        private float _elapsedTransportTime;

        private void Awake()
        {
            if (!InstanceFinder.TimeManager)
            {
                Debug.LogError("No TimeManager found! Ticks won't work!");
                return;
            }

            InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        }
        
        private void OnDisable()
        {
            if (!InstanceFinder.TimeManager) return;
            InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            Players.Add(this);
            Players.Sort((x, y) => x.OwnerId.CompareTo(y.OwnerId));
            OnPlayerAdded?.Invoke(this);
            
            leftLaserPointer.SetActive(!IsOwner);
            rightLaserPointer.SetActive(!IsOwner);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            Players.Remove(this);
            Players.Sort((x, y) => x.OwnerId.CompareTo(y.OwnerId));
            OnPlayerRemoved?.Invoke(this);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _syncedTransporting.OnChange += OnTransportingStateChanged;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _syncedTransporting.OnChange -= OnTransportingStateChanged;
        }

        private void Update()
        {
            if (IsServerInitialized)
            {
                //Disconnect timer
                if (!autoDisconnect) return; 
                if (_lastHeadPosition != clientHeadTransform.position)
                {
                    _disconnectTimer = 0;
                    _lastHeadPosition = clientHeadTransform.position;
                }
                else
                {
                    _disconnectTimer += Time.deltaTime;
                }
                if (_disconnectTimer > disconnectTime)
                {
                    if (Owner != null) Owner.Disconnect(true);
                }
            }

            if (IsOwner)
            {
                // Debug.Log("XR Player is owner", this);
                
                if (Transporting)
                {
                    if (!ReadyForTransport && _elapsedTransportTime <= _transportSequenceDuration)
                    {
                        Debug.Log("Transport request was received and ready timer is running", this);
                        //Transport request was received and ready timer is running
                        _elapsedTransportTime += Time.deltaTime;
                    }

                    if (!ReadyForTransport && _elapsedTransportTime > _transportSequenceDuration)
                    {
                        Debug.Log("Ready timer has reached it's limit, set ReadyForTransport to true", this);
                        //Ready timer has reached its limit, set ReadyForTransport to true
                        SetReadyForTransportState(true);
                    }
                }
                else if (!Transporting && ReadyForTransport)
                {
                    Debug.Log("Transport is finished but ReadyForTransport needs to be reset after a transport", this);
                    //Transport is finished but ReadyForTransport needs to be reset after a transport
                    SetReadyForTransportState(false);
                }
            }
        }

        public void DisconnectPlayer()
        {
            if (Owner != null)
            {
                DisconnectPlayer(Owner);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DisconnectPlayer(NetworkConnection conn)
        {
            conn.Disconnect(false);
        }
        
        private void TimeManager_OnTick()
        {
            if (IsOwner)
            {
                if (XROriginReferences.Instance == null)
                {
                    Debug.LogError("Could not find XR Rig");
                    return;
                }
                
                //Update local XRRigReferences positions and rotations to sync them to the server 
                clientHeadTransform.SetPositionAndRotation(XROriginReferences.Instance.head.position, XROriginReferences.Instance.head.rotation);
                clientLeftHandTransform.SetPositionAndRotation(XROriginReferences.Instance.handLeft.position, XROriginReferences.Instance.handLeft.rotation);
                clientRightHandTransform.SetPositionAndRotation(XROriginReferences.Instance.handRight.position, XROriginReferences.Instance.handRight.rotation);
                
                //Update Teleport Pointers
                if (XROriginReferences.Instance.TeleportPointerLeftOn != LeftTeleportPointerOn)
                    SetLeftTeleportPointerOn(XROriginReferences.Instance.TeleportPointerLeftOn);

                if (XROriginReferences.Instance.TeleportPointerRightOn != RightTeleportPointerOn)
                    SetRightTeleportPointerOn(XROriginReferences.Instance.TeleportPointerRightOn);
            }
            
            //Update on observers
            leftTeleportPointer.SetActive(LeftTeleportPointerOn && !IsOwner);
            rightTeleportPointer.SetActive(RightTeleportPointerOn && !IsOwner);
        }

        [ServerRpc (RunLocally = true)]
        private void SetReadyForTransportState(bool newState)
        {
             _syncedReadyForTransport.Value = newState;
        }

        private void OnTransportingStateChanged(bool prev, bool next, bool asServer)
        {
            if (asServer) return;
            if (!IsOwner) return;
            Debug.Log("Set Player " + this + " ready for transport state to " + next + "!");
            if (next)
            {
                _elapsedTransportTime = 0;
                ScreenFade.Instance.FadeOut();
            }
        }

        [ServerRpc(RunLocally = true)]
        public void SetTransporting(bool newTransporting)
        {
            _syncedTransporting.Value = newTransporting;
        }

        public void TransportAndTeleportCompleted()
        {
            StartCoroutine(ScreenFadeInDelayRoutine(0.5f));
        }

        private IEnumerator ScreenFadeInDelayRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ScreenFade.Instance.FadeIn();
            yield return null;
        }
    }
}
