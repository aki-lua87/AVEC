using System;
using UnityEngine;

namespace aki_lua87.CommonGestureRemote
{
    public enum RemoteAction
    {
        NextVideo = 1, PreviousVideo = 2, VolumeUp = 3, VolumeDown = 4,
        StopPlayback = 8, TogglePlaybackMode = 9, NextTab = 10, PreviousTab = 11,
        Pause = 12, Resume = 13, MuteToggle = 14, CustomEvent = 15, RepeatToggle = 16,
        Restart = 17, OrderedPlayback = 18, RepeatOne = 19, ShufflePlayback = 20,
        PlaybackToggle = 21, SummonToggle = 22
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
            new GestureBinding { gesture = "→←→", label = "次の動画", action = RemoteAction.NextVideo },
            new GestureBinding { gesture = "←→←", label = "前の動画", action = RemoteAction.PreviousVideo },
            new GestureBinding { gesture = "→↑", label = "音量を上げる", action = RemoteAction.VolumeUp },
            new GestureBinding { gesture = "→↓", label = "音量を下げる", action = RemoteAction.VolumeDown },
            new GestureBinding { gesture = "↓↑", label = "ミュート／解除", action = RemoteAction.MuteToggle },
            new GestureBinding { gesture = "↑↓", label = "一時停止／再生・再開", action = RemoteAction.PlaybackToggle },
            new GestureBinding { gesture = "←↓↑", label = "1動画リピートに設定", action = RemoteAction.RepeatOne },
            new GestureBinding { gesture = "←↓→", label = "順送り再生に設定", action = RemoteAction.OrderedPlayback },
            new GestureBinding { gesture = "←↓←", label = "シャッフル再生に設定", action = RemoteAction.ShufflePlayback },
            new GestureBinding { gesture = "↑←↓", label = "現在の動画を初めから再生", action = RemoteAction.Restart },
            new GestureBinding { gesture = "↑→↓", label = "呼び出し ON/OFF", action = RemoteAction.SummonToggle }
        };
    }
}
