using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace aki_lua87.AVEC.Editor
{
    public sealed class ConnectorDefinition
    {
        public string Id, DisplayName, PlayerType, PlaylistAdapterType, RemoteTargetType;
        // Compatibility is tied to the native API major, not the installed package's latest version.
        public string SupportedMajorVersion;
        public int Order;
        public string AuthorityField;
        public Func<Component, Component> ResolveAuthority;
        public Action<Component, Component> ConfigurePlaylist, ConfigureRemote;
        public Action<Component, Component> DiscoverRemoteOutputs;
        public Func<Component, Component, string> ValidatePlaylist;
        public Component Authority(Component player)
        {
            if (ResolveAuthority != null) return ResolveAuthority(player);
            if (player == null || string.IsNullOrEmpty(AuthorityField)) return player;
            var field = player.GetType().GetField(AuthorityField, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? field.GetValue(player) as Component : new SerializedObject(player).FindProperty(AuthorityField)?.objectReferenceValue as Component;
        }
    }
    public static class ConnectorRegistry
    {
        static readonly Dictionary<string, ConnectorDefinition> entries = new Dictionary<string, ConnectorDefinition>();
        public static ConnectorDefinition[] All => entries.Values.OrderBy(d => d.Order).ThenBy(d => d.Id, StringComparer.Ordinal).ToArray();
        public static void Register(ConnectorDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id) || string.IsNullOrEmpty(definition.PlayerType)) throw new ArgumentException("Connector definition is incomplete.");
            if (entries.ContainsKey(definition.Id)) throw new InvalidOperationException("Duplicate connector: " + definition.Id);
            entries.Add(definition.Id, definition);
        }
    }
}
