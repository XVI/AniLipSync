using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XVI.AniLipSync {
    public class AnimMorphTarget : MonoBehaviour {
        [Tooltip("aa, E, ih, oh, ou のそれぞれの音素へ遷移する際に、BlendShapeの重みを時間をかけて変化させるためのカーブ")]
        public AnimationCurve[] transitionCurves = new AnimationCurve[5];

        [Tooltip("カーブの値をBlendShapeに適用する際の倍率")]
        public float curveAmplifier = 100.0f;

        [Range(0.0f, 100.0f), Tooltip("この閾値未満の音素の重みは無視する")]
        public float weightThreashold = 2.0f;

        [Tooltip("BlendShapeの重みを変化させるフレームレート")]
        public float frameRate = 12.0f;

        [Tooltip("BlendShapeの値を変化させるSkinnedMeshRenderer")]
        public SkinnedMeshRenderer skinnedMeshRenderer;

        [Tooltip("aa, E, ih, oh, ouの順で割り当てるBlendShapeのindex")]
        public int[] visemeToBlendShape = new int[5];

        [Tooltip("長音時、このRMS値（音量）より大きければ口の形を維持する")]
        public float rmsThreshold = 0.005f;

        OVRLipSyncContextBase context;
        LowLatencyLipSyncContext lowLatencyLipSyncContext;
        OVRLipSync.Viseme previousViseme = OVRLipSync.Viseme.sil;
        float transitionTimer = 0.0f;
        float frameRateTimer = 0.0f;

        void Start() {
            if (skinnedMeshRenderer == null) {
                Debug.LogError("SkinnedMeshRendererが指定されていません。", this);
            }

            context = GetComponent<OVRLipSyncContextBase>();
            if (context == null) {
                Debug.LogError("同じGameObjectにOVRLipSyncContextBaseを継承したクラスが見つかりません。", this);
            }

            // LowLatencyLipSyncContext以外でも動くようにしておくこと
            lowLatencyLipSyncContext = context as LowLatencyLipSyncContext;
        }

        void Update() {
            if (context == null || skinnedMeshRenderer == null) {
                return;
            }

            var frame = context.GetCurrentPhonemeFrame();
            if (frame == null) {
                return;
            }

            transitionTimer += Time.deltaTime;

            // 設定したフレームレートへUpdate関数を低下させる
            frameRateTimer += Time.deltaTime;
            if (frameRateTimer < 1.0f / frameRate) {
                return;
            }
            frameRateTimer -= 1.0f / frameRate;

            // すでに設定されているBlendShapeの重みをリセット
            foreach (var blendShape in visemeToBlendShape) {
                if (blendShape < 0) {
                    continue;
                }

                skinnedMeshRenderer.SetBlendShapeWeight(blendShape, 0.0f);
            }

            // 最大の重みを持つ音素を探す
            var maxVisemeIndex = 0;
            var maxVisemeWeight = frame.Visemes[0];
            // 子音は無視する
            for (var i = (int)OVRLipSync.Viseme.aa; i < frame.Visemes.Length; i++) {
                if (frame.Visemes[i] > maxVisemeWeight) {
                    maxVisemeWeight = frame.Visemes[i];
                    maxVisemeIndex = i;
                }
            }

            // 音素の重みが小さすぎる場合は、口の形を維持する
            if (maxVisemeWeight * 100.0f < weightThreashold) {
                maxVisemeIndex = (int)previousViseme;
            }

            // 長音時に口が閉じてしまわないように、閾値より大きければ前フレームの口の形を維持する
            if (lowLatencyLipSyncContext != null && maxVisemeIndex == (int)OVRLipSync.Viseme.sil && lowLatencyLipSyncContext.GetMicVolume() > rmsThreshold) {
                maxVisemeIndex = (int)previousViseme;
            }

            // 音素の切り替わりでタイマーをリセットする
            if (previousViseme != (OVRLipSync.Viseme)maxVisemeIndex) {
                transitionTimer = 0.0f;
                previousViseme = (OVRLipSync.Viseme)maxVisemeIndex;
            }

            // 無音の場合はBlendShapeの値は上でリセットしたのでゼロ
            if (maxVisemeIndex == (int)OVRLipSync.Viseme.sil) {
                previousViseme = OVRLipSync.Viseme.sil;
                return;
            }

            var visemeIndex = maxVisemeIndex - (int)OVRLipSync.Viseme.aa;
            skinnedMeshRenderer.SetBlendShapeWeight(visemeToBlendShape[visemeIndex], transitionCurves[visemeIndex].Evaluate(transitionTimer) * curveAmplifier);
        }
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(AnimMorphTarget))]
    public class AnimMorphTargetEditor : Editor {
        static readonly string[] visemeNames = new string[] { "sil(無音)", "PP(子音)", "FF(子音)", "TH(子音)", "DD(子音)", "kk(子音)", "CH(子音)", "SS(子音)", "nn(ん)", "RR(子音)", "aa(あ)", "E(え)", "ih(い)", "oh(お)", "ou(う)" };

        OVRLipSyncContextBase lipSyncContext;

        public override bool HasPreviewGUI() {
            return true;
        }

        public override bool RequiresConstantRepaint() {
            return false;
        }

        public override GUIContent GetPreviewTitle() {
            return new GUIContent("AnimMorphTarget Debug Preview");
        }

        void OnEnable() {
            var targetComponent = target as AnimMorphTarget;
            lipSyncContext = targetComponent.GetComponent<OVRLipSyncContextBase>();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            base.OnPreviewGUI(r, background);

            var animMorphTargetType = (target as AnimMorphTarget).GetType();
            var field = animMorphTargetType.GetField("previousViseme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var previousViseme = (int)(OVRLipSync.Viseme)field.GetValue(target);

            var frame = lipSyncContext.GetCurrentPhonemeFrame();
            var visemeIndex = 0;

            var whiteStyle = new GUIStyle();
            whiteStyle.normal.textColor = Color.white;
            var redStyle = new GUIStyle();
            redStyle.normal.textColor = Color.magenta;

            foreach (var visemeName in visemeNames) {
                var labelRect = new Rect(r) {
                    width = 100,
                    height = EditorGUIUtility.singleLineHeight
                };
                r.y += labelRect.height;

                var style = whiteStyle;
                if (visemeIndex == previousViseme) {
                    style = redStyle;
                }
                GUI.Label(labelRect, visemeName, style);

                var weight = frame.Visemes[visemeIndex];
                var weightRect = new Rect(labelRect) {
                    x = labelRect.x + labelRect.width
                };
                GUI.Label(weightRect, weight.ToString("0.000"), whiteStyle);
                visemeIndex++;
            }
            r.y += 10;

            var rmsProvider = (target as AnimMorphTarget).GetComponent<LowLatencyLipSyncContext>();
            if (rmsProvider != null) {
                var rmsLabelRect = new Rect(r) {
                    width = 100,
                    height = EditorGUIUtility.singleLineHeight
                };
                GUI.Label(rmsLabelRect, "RMS", whiteStyle);
                var rms = rmsProvider.GetMicVolume();
                var rmsRect = new Rect(r) {
                    x = rmsLabelRect.x + rmsLabelRect.width,
                    width = 100,
                    height = EditorGUIUtility.singleLineHeight
                };
                GUI.Label(rmsRect, rms.ToString("0.00000"), whiteStyle);
                r.y += rmsLabelRect.height;
            }
        }
    }
    #endif
}
