using System.Collections.Generic;
using System.Text;
using ColossalFramework.Plugins;

namespace SkylinesAgentBridge
{
    public static class ModCommands
    {
        public static CommandResult BuildModsJson(int limit)
        {
            if (limit < 0)
            {
                limit = 0;
            }
            if (limit > 1000)
            {
                limit = 1000;
            }

            StringBuilder items = new StringBuilder();
            int total = 0;
            int emitted = 0;
            bool first = true;

            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                if (plugin == null)
                {
                    continue;
                }

                total++;
                if (emitted >= limit)
                {
                    continue;
                }

                if (!first)
                {
                    items.Append(",");
                }
                AppendPlugin(items, plugin);
                first = false;
                emitted++;
            }

            return CommandResult.FromJson("{\"ok\":true,\"total\":" + total +
                ",\"returned\":" + emitted +
                ",\"limit\":" + limit +
                ",\"mods\":[" + items.ToString() + "]}");
        }

        public static CommandResult SetModEnabled(string body)
        {
            string name = JsonUtil.GetString(body, "name", "").Trim();
            bool enabled = JsonUtil.GetBool(body, "enabled", false);

            if (name.Length == 0)
            {
                return CommandResult.Fail("name is required. Check /state/mods for exact plugin/display names.");
            }

            PluginManager.PluginInfo exact = null;
            List<PluginManager.PluginInfo> partial = new List<PluginManager.PluginInfo>();

            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                if (plugin == null)
                {
                    continue;
                }

                string displayName = GetDisplayName(plugin);
                bool nameExact = string.Equals(plugin.name, name, System.StringComparison.OrdinalIgnoreCase);
                bool displayExact = displayName != null && string.Equals(displayName, name, System.StringComparison.OrdinalIgnoreCase);

                if (nameExact || displayExact)
                {
                    exact = plugin;
                    break;
                }

                bool nameContains = plugin.name != null && plugin.name.ToLowerInvariant().Contains(name.ToLowerInvariant());
                bool displayContains = displayName != null && displayName.ToLowerInvariant().Contains(name.ToLowerInvariant());
                if (nameContains || displayContains)
                {
                    partial.Add(plugin);
                }
            }

            PluginManager.PluginInfo target = exact;
            if (target == null)
            {
                if (partial.Count == 1)
                {
                    target = partial[0];
                }
                else if (partial.Count > 1)
                {
                    StringBuilder candidates = new StringBuilder();
                    bool first = true;
                    foreach (PluginManager.PluginInfo plugin in partial)
                    {
                        if (!first)
                        {
                            candidates.Append(", ");
                        }
                        candidates.Append(GetDisplayName(plugin) ?? plugin.name);
                        first = false;
                    }
                    return CommandResult.Fail("Multiple mods match '" + name + "': " + candidates.ToString() + ". Use a more specific name.");
                }
            }

            if (target == null)
            {
                return CommandResult.Fail("No mod matching '" + name + "' was found. Check /state/mods.");
            }

            bool before = target.isEnabled;
            target.isEnabled = enabled;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"name\":\"").Append(JsonUtil.Escape(target.name)).Append("\"");
            json.Append(",\"displayName\":\"").Append(JsonUtil.Escape(GetDisplayName(target) ?? "")).Append("\"");
            json.Append(",\"wasEnabled\":").Append(JsonUtil.Bool(before));
            json.Append(",\"isEnabled\":").Append(JsonUtil.Bool(target.isEnabled));
            json.Append(",\"message\":\"Toggled live and persisted. OnEnabled/OnDisabled was invoked via reflection if the mod implements it, but some native/Harmony hooks may only fully clear after a restart.\"");
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static void AppendPlugin(StringBuilder items, PluginManager.PluginInfo plugin)
        {
            items.Append("{\"name\":\"").Append(JsonUtil.Escape(plugin.name)).Append("\"");
            items.Append(",\"displayName\":\"").Append(JsonUtil.Escape(GetDisplayName(plugin) ?? "")).Append("\"");
            items.Append(",\"isEnabled\":").Append(JsonUtil.Bool(plugin.isEnabled));
            items.Append(",\"isBuiltin\":").Append(JsonUtil.Bool(plugin.isBuiltin));
            items.Append(",\"isCameraScript\":").Append(JsonUtil.Bool(plugin.isCameraScript));
            items.Append(",\"assemblyCount\":").Append(plugin.assemblyCount);
            items.Append(",\"modPath\":\"").Append(JsonUtil.Escape(plugin.modPath)).Append("\"");
            items.Append("}");
        }

        private static string GetDisplayName(PluginManager.PluginInfo plugin)
        {
            object userMod = plugin.userModInstance;
            ICities.IUserMod typed = userMod as ICities.IUserMod;
            return typed != null ? typed.Name : null;
        }
    }
}
