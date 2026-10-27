# KineL V3 connector

Targets the KineL Video Player V3 API. Verified in this project against
`la.niri.videoplayer` 3.4.3, using C# and UdonSharp compilation and edit-time
scene wiring checks. Playback and multiplayer verification remain manual.

- Playlist: `KinelV3Adapter` shares the native `KinelVariableSyncer` GameObject.
- Events: `KinelV3SequenceListener` registers through `KinelPlayerController.AddListener`.
- Remote: `KinelV3RemoteTarget` uses the same controller, syncer and audio manager.
- Volume and mute use `KinelAudioManager`, so its state stays authoritative locally.
- Playback lock checks use V3's `CanOperate`, including its permission provider.
- Common playlist navigation uses the Core-bundled PlaylistRemote integration. Native
  V3 playlist navigation is not exposed by this connector.

The native player is an external dependency. Its scripts and prefabs are neither
modified nor bundled. Release Builder exports Playlist and Remote together as `AVEC-Connector-KineLV3`;
creating a release remains an explicit operation.

## Compatibility policy

Create separate connectors when a native player's major version changes its API,
ownership authority or event contract. Keep common UI/runtime code independent
of that version. Minor/patch versions can share a connector after verification;
they do not need separate classes or folders by default.

`ConnectorDefinition.SupportedMajorVersion` records the supported API major.
KineL V2 retains its existing `KineL` connector ID, directory, class names and
asset GUIDs for serialized-reference compatibility. Its display name is now
`KineL V2`; the new connector ID is `KineLV3`. ProTV records major version `3`
and displays `ProTV V3`. Do not infer KineL V2's API from the installed package
version: package 3.4.3 contains both the legacy V2 and V3 implementations.

## Demo scene

`CommonAssetsDemo` contains separate KineL V2 and KineL V3 stations. V3 is in
room 06 (corridor right, rear), with a wall playlist, tablet and remote. Its
dedicated `Demo/Playlists/KinelV3PL.asset` starts as a copy of the current V2
playlist data. Existing UI layout, theme and video entries were copied without
rebaking the existing scene or regenerating prefabs.
