using ICities;

namespace SkylinesAgentBridge
{
    public sealed class AgentBridgeMod : IUserMod
    {
        public string Name
        {
            get { return "Skylines Agent Bridge"; }
        }

        public string Description
        {
            get { return "Localhost API bridge for AI agents to inspect and build in Cities: Skylines."; }
        }

        // Not part of ICities.IUserMod: CS1's PluginManager finds and invokes these by
        // reflection (method name convention) whenever the mod is enabled/disabled,
        // including once at game boot for mods that are already enabled — well before
        // any city is loaded. This is what lets the API listen at the main menu.
        public void OnEnabled()
        {
            AgentBridge.Instance.OnModEnabled();
        }

        public void OnDisabled()
        {
            AgentBridge.Instance.OnModDisabled();
        }
    }
}
