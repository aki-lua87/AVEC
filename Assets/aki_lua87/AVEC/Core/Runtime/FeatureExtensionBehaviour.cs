using UdonSharp;

namespace aki_lua87.AVEC
{
    // Optional runtime hooks. Core does not reference individual features or players.
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FeatureExtensionBehaviour : UdonSharpBehaviour
    {
        public abstract void _OnSelectionChanged(int index);
    }
}
