using UnityEngine;

namespace SkylinesAgentBridge
{
    public sealed class AgentBridge
    {
        private static readonly AgentBridge singleton = new AgentBridge();
        private readonly CommandQueue queue = new CommandQueue();
        private ApiServer server;
        private bool levelLoaded;
        private GameObject pumpObject;

        public static AgentBridge Instance
        {
            get { return singleton; }
        }

        public bool LevelLoaded
        {
            get { return levelLoaded; }
        }

        public CommandQueue Queue
        {
            get { return queue; }
        }

        public void OnModEnabled()
        {
            EnsureServer();
            EnsurePump();
            Debug.Log("[SkylinesAgentBridge] Mod enabled. API bridge listening (no city loaded yet).");
        }

        public void OnModDisabled()
        {
            StopServer();
            StopPump();
            Debug.Log("[SkylinesAgentBridge] Mod disabled. API bridge stopped.");
        }

        public void OnLevelLoaded()
        {
            levelLoaded = true;
            EnsureServer();
            AgentBridgeNotifier.Notify("API ready: Skylines Agent Bridge");
            Debug.Log("[SkylinesAgentBridge] Level loaded. API bridge is ready.");
        }

        public void OnLevelUnloading()
        {
            levelLoaded = false;
            queue.Clear();
            AgentBridgeNotifier.Destroy();
            Debug.Log("[SkylinesAgentBridge] Level unloading. Pending API commands cleared.");
        }

        public void ProcessGameThreadQueue(float realTimeDelta)
        {
            queue.Process(4);
            AgentBridgeNotifier.Update(realTimeDelta);
        }

        private void EnsureServer()
        {
            if (server != null && server.IsRunning)
            {
                return;
            }

            server = new ApiServer(this, 32123);
            server.Start();
        }

        private void StopServer()
        {
            if (server != null)
            {
                server.Stop();
            }
        }

        private void EnsurePump()
        {
            if (pumpObject != null)
            {
                return;
            }

            pumpObject = new GameObject("SkylinesAgentBridgePump");
            pumpObject.AddComponent<AgentBridgePump>();
            UnityEngine.Object.DontDestroyOnLoad(pumpObject);
        }

        private void StopPump()
        {
            if (pumpObject != null)
            {
                UnityEngine.Object.Destroy(pumpObject);
                pumpObject = null;
            }
        }
    }
}
