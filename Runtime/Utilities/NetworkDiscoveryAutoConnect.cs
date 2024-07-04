using System.Collections;
using System.Collections.Generic;
using System.Net;
using FishNet.Managing;
using UnityEngine;

namespace _OOI.FishNet
{
    public class NetworkDiscoveryAutoConnect : MonoBehaviour
    {
        public float searchInterval = 2;
        private NetworkDiscovery _networkDiscovery;
        private NetworkManager _networkManager;

        private readonly List<IPEndPoint> _endPoints = new List<IPEndPoint>();

        public static NetworkDiscoveryAutoConnect Instance
        {
            get
            {
                if (!_instance) _instance = FindObjectOfType<NetworkDiscoveryAutoConnect>(true);
                return _instance;
            }
        }

        private static NetworkDiscoveryAutoConnect _instance;

        private void Awake()
        {
            _networkDiscovery = GetComponent<NetworkDiscovery>();
            _networkManager = GetComponent<NetworkManager>();
        }

        // Start is called before the first frame update
        void Start()
        {
            if (_networkManager.IsServerStarted) return;
            if (!_networkManager.IsOffline) return;

            StartSearching();
        }

        public void StartSearching()
        {
            if (_networkManager.IsServerStarted) return;
            if (!_networkManager.IsOffline) return;

            // Debug.LogWarning("Starting auto server discovery");
            _networkDiscovery.ServerFoundCallback += (endPoint) =>
            {
                if (!_endPoints.Contains(endPoint)) _endPoints.Add(endPoint);
            };
            
            if (gameObject.activeSelf) StartCoroutine(SearchCoroutine());
        }

        private IEnumerator SearchCoroutine()
        {
            _networkDiscovery.StopSearchingForServers();
            if (!_networkManager.IsOffline) yield return null;

            yield return new WaitForEndOfFrame();
            _networkDiscovery.StartSearchingForServers();

            float timer = 0;
            while (timer < searchInterval)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (_networkManager.IsOffline && !_networkManager.IsServerStarted)
            {
                // Debug.Log("Restart searching for servers...");
                StartCoroutine(SearchCoroutine());
                yield return null;
            }
            
            if (_endPoints.Count > 0)
            {
                _networkManager.ClientManager.StartConnection(_endPoints[0].Address.ToString());
                yield return null;
            }
            
            yield return null;
        }
    }
}
