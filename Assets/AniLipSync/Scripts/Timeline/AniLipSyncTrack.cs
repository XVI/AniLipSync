using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace XVI.AniLipSync
{
    [TrackBindingType(typeof(SkinnedMeshRenderer))]
    [TrackClipType(typeof(AniLipSyncClip))]
    public class AniLipSyncTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var c in GetClips())
            {
                AniLipSyncClip clipAsset = c.asset as AniLipSyncClip;
                clipAsset.skinnedMeshRenderer = (SkinnedMeshRenderer)go.GetComponent<PlayableDirector>().GetGenericBinding(this);
                clipAsset.owningClip = c;
                if (clipAsset.syncSequence != null)
                {
                    c.duration = clipAsset.syncSequence.length;
                }
            }

            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }
}

