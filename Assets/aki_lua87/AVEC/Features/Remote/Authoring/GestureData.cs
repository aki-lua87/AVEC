using System;
using UnityEngine;

namespace aki_lua87.CommonGestureRemote
{
    public enum RemoteAction
    {
        NextVideo = 1, PreviousVideo = 2, VolumeUp = 3, VolumeDown = 4,
        StopPlayback = 8, TogglePlaybackMode = 9, NextTab = 10, PreviousTab = 11,
        Pause = 12, Resume = 13, MuteToggle = 14, CustomEvent = 15, RepeatToggle = 16
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
            new GestureBinding { gesture = "↑↓", label = "ミュート切替", action = RemoteAction.MuteToggle },
            new GestureBinding { gesture = "↓↑", label = "1動画リピート切替", action = RemoteAction.RepeatToggle }
        };
    }
}
