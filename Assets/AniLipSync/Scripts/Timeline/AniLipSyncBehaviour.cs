using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace XVI.AniLipSync
{
    public class AniLipSyncBehaviour : PlayableBehaviour
    {
        public AniLipSyncClip clipAsset = null;

        public override void ProcessFrame(UnityEngine.Playables.Playable playable, UnityEngine.Playables.FrameData info, object playerData)
        {
            if (clipAsset.syncSequence == null) return;

            var time = playable.GetTime();
            var frame = clipAsset.syncSequence.GetFrameAtTime((float)time);

            // 最大の重みを持つ音素を探す
            var maxVisemeIndex = 0;
            var maxVisemeWeight = 0.0f;
            // 子音は無視する
            for (var i = (int)OVRLipSync.Viseme.aa; i < frame.Visemes.Length; i++)
            {
                if (frame.Visemes[i] > maxVisemeWeight)
                {
                    maxVisemeWeight = frame.Visemes[i];
                    maxVisemeIndex = i;
                }
            }

            if (maxVisemeWeight * 100.0f < clipAsset.weightThreashold)
            {
                return;
            }

            var visemeIndex = maxVisemeIndex - (int)OVRLipSync.Viseme.aa;

            clipAsset.SetVisemeToBlendShap(visemeIndex,maxVisemeWeight);
        }


        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (clipAsset != null)
            {
                clipAsset.ResetBlendShape();
            }
        }
    }
}

