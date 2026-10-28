using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;
using aki_lua87.AVEC.Editor;

namespace aki_lua87.AVEC.SimpleScreen.Editor
{
    // Edit-time checks only. Does not invoke any runtime method, load URLs, or mutate scenes.
    public static class SimpleScreenValidation
    {
        [MenuItem("Tools/AVEC/SimpleScreen/Validate Prefab")]
        public static void ValidateMenu() { Debug.Log(Validate()); }
        public static string Validate()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SimpleScreenBuilder.PrefabPath);
            Require(prefab!=null,"Distribution prefab is missing.");
            Require(prefab.GetComponentsInChildren<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"Missing script.");
            var player=prefab.GetComponent<SimpleScreenPlayer>();
            Require(player!=null&&player.videoPlayer!=null&&player.screen!=null,"Player/video/screen reference is missing.");
            Require(player.adapter!=null&&player.adapter.player==player&&player.adapter.gameObject==player.gameObject,"Playlist authority must share the player GameObject.");
            Require(player.adapter.views.Length==0,"Distribution template must not contain playlist data.");
            Require(player.speakers.Length>0&&player.speakers.All(s=>s!=null&&s.GetComponent<VRC.SDK3.Components.VRCSpatialAudioSource>()!=null&&s.GetComponent<VRC.SDK3.Video.Components.AVPro.VRCAVProVideoSpeaker>()?.VideoPlayer==player.videoPlayer),"Speaker routing or spatial audio is missing.");
            Require(player.screen.GetComponent<VRC.SDK3.Video.Components.AVPro.VRCAVProVideoScreen>()?.VideoPlayer==player.videoPlayer,"Screen video routing is missing.");
            var ui=player.controls;
            Require(ui!=null&&ui.player==player&&ui.panel!=null&&ui.seek!=null&&ui.volume!=null&&ui.urlInput!=null,"Control references are missing.");
            Require(ui.GetComponent<Canvas>()?.renderMode==RenderMode.WorldSpace&&ui.gameObject.layer==0&&ui.GetComponent<VRC.SDK3.Components.VRCUiShape>()!=null,"World UI canvas/shape/layer is invalid.");
            Require(ui.soundSprite!=null&&ui.mutedSprite!=null&&ui.muteIcon!=null,"Mute icon references are missing.");
            Require(ui.seek.maxValue==1f&&ui.volume.maxValue==1f&&ui.urlInput.characterLimit==2048,"Control ranges are invalid.");
            var backing=UdonSharpEditorUtility.GetBackingUdonBehaviour(ui);
            CheckEvent(ui.seek, "m_OnValueChanged",ui.seek.onValueChanged,backing,nameof(SimpleScreenUI._SeekChanged));
            CheckEvent(ui.volume,"m_OnValueChanged",ui.volume.onValueChanged,backing,nameof(SimpleScreenUI._VolumeChanged));
            CheckEvent(ui.urlInput,"m_OnEndEdit",ui.urlInput.onEndEdit,backing,nameof(SimpleScreenUI._SubmitUrl));
            Require(prefab.GetComponentsInChildren<Button>(true).All(b=>b.onClick.GetPersistentEventCount()>0),"Button action is missing.");
            var entries=prefab.GetComponentsInChildren<EventTrigger>(true).SelectMany(t=>t.triggers).ToArray();
            Require(entries.Any(e=>e.eventID==EventTriggerType.PointerEnter)&&entries.Any(e=>e.eventID==EventTriggerType.PointerExit)&&entries.Any(e=>e.eventID==EventTriggerType.PointerDown)&&entries.Any(e=>e.eventID==EventTriggerType.PointerUp)&&entries.Any(e=>e.eventID==EventTriggerType.Select),"Hover/drag/input callbacks are missing.");
            Require(entries.All(e=>e.callback.GetPersistentEventCount()>0&&e.callback.GetPersistentTarget(0)==backing),"EventTrigger points to the wrong Udon behaviour.");
            foreach(var guid in AssetDatabase.FindAssets("t:UdonSharpProgramAsset",new[]{SimpleScreenBuilder.Root+"/Runtime"}))
            {
                var program=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(AssetDatabase.GUIDToAssetPath(guid));
                Require(program.sourceCsScript!=null&&program.SerializedProgramAsset!=null,"Compiled Udon program is missing.");
                Require(AssetDatabase.GetAssetPath(program.SerializedProgramAsset).StartsWith(SimpleScreenBuilder.Root+"/Runtime/Programs/",StringComparison.Ordinal),"Compiled program is outside the Core feature folder.");
                var data=new SerializedObject(program);
                Require(string.IsNullOrEmpty(data.FindProperty("assemblyError").stringValue),"Udon assembly has an error.");
            }
            Require(!UdonSharpProgramAsset.AnyUdonSharpScriptHasError(),"UdonSharp compile error.");
            var connector=ConnectorRegistry.All.FirstOrDefault(d=>d.Id=="SimpleScreen");
            Require(connector!=null&&connector.PlayerType==typeof(SimpleScreenPlayer).FullName&&connector.PlaylistAdapterType==typeof(SimpleScreenAdapter).FullName&&connector.RemoteTargetType==typeof(SimpleScreenRemoteTarget).FullName,"Connector registration is invalid.");
            var dependencies=AssetDatabase.GetDependencies(SimpleScreenBuilder.PrefabPath,true);
            Require(!dependencies.Any(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&!p.StartsWith("Assets/aki_lua87/AVEC/",StringComparison.Ordinal)),"Prefab depends on an external player or development asset.");
            return "SimpleScreen: prefab, compiled Udon, UI callbacks, audio/video, connector, and dependency checks passed. No runtime test executed.";
        }
        static void CheckEvent(Component component,string property,UnityEngine.Events.UnityEventBase evt,UdonBehaviour backing,string method)
        {
            Require(evt.GetPersistentEventCount()==1&&evt.GetPersistentTarget(0)==backing&&evt.GetPersistentMethodName(0)=="SendCustomEvent","Invalid UI event: "+method);
            // Unity serializes the constant string argument independently of the method name.
            var argument=new SerializedObject(component).FindProperty(property+".m_PersistentCalls.m_Calls.Array.data[0].m_Arguments.m_StringArgument");
            Require(argument!=null&&argument.stringValue==method&&typeof(SimpleScreenUI).GetMethod(method)!=null,"UI handler is missing or miswired: "+method);
        }
        static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException("SimpleScreen: "+message); }
    }
}
