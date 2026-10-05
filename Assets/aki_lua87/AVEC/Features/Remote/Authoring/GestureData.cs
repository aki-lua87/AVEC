using System;
using UnityEngine;

namespace aki_lua87.CommonGestureRemote
{
    public enum RemoteAction
    {
        NextVideo = 1, PreviousVideo = 2, VolumeUp = 3, VolumeDown = 4,
        LocalPowerOff = 5, LocalPowerOn = 6, LocalPowerToggle = 7,
        StopPlayback = 8, TogglePlaybackMode = 9, NextTab = 10, PreviousTab = 11,
        Pause = 12, Resume = 13, MuteToggle = 14, CustomEvent = 15
    }
    [Serializable]
    public class GestureBinding
    {
        public string gesture;
        public string label;
        public RemoteAction action;
        public string customEvent;
    }
    [CreateAssetMenu(menuName = "AVEC/Remote/Gesture Data")]
    public class GestureData : ScriptableObject
    {
        public GestureBinding[] bindings = {
            new GestureBinding { gesture = "→", label = "次の動画", action = RemoteAction.NextVideo },
            new GestureBinding { gesture = "←", label = "前の動画", action = RemoteAction.PreviousVideo },
            new GestureBinding { gesture = "→↑", label = "音量 +10%", action = RemoteAction.VolumeUp },
            new GestureBinding { gesture = "→↓", label = "音量 -10%", action = RemoteAction.VolumeDown },
            new GestureBinding { gesture = "↑↓", label = "プレイヤー OFF（自分）", action = RemoteAction.LocalPowerOff },
            new GestureBinding { gesture = "↓↑", label = "プレイヤー ON（自分）", action = RemoteAction.LocalPowerOn }
        };
    }
}
