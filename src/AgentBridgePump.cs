using UnityEngine;

namespace SkylinesAgentBridge
{
    // AgentBridgeThreading (ICities.ThreadingExtensionBase) only ticks once
    // LoadingManager.m_loadingComplete is true, which in practice only happens after a
    // city/level finishes loading, not just the main menu shell. That leaves the
    // CommandQueue undrained (and any RunAlways request hanging until it times out) for
    // as long as the game sits at the main menu. This plain MonoBehaviour gives the
    // bridge its own always-on Update() tick, independent of level/simulation state, so
    // menu-safe commands (state/mods, state/saves, commands/load-save, commands/quit,
    // commands/set-mod-enabled) work before any city is loaded.
    public sealed class AgentBridgePump : MonoBehaviour
    {
        private void Update()
        {
            AgentBridge.Instance.ProcessGameThreadQueue(Time.deltaTime);
        }
    }
}
