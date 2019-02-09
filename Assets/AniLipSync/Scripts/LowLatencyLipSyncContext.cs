using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XVI.AniLipSync {
    public class LowLatencyLipSyncContext : OVRLipSyncContextBase {
        [Tooltip("Microphone input gain. (Amplitude ratio, no unit.)")]
        [SerializeField] private float gain = 1.0f;

        [HideInInspector] public int deviceIndex = 0;

        [HideInInspector] public string selectedDevice;

        AudioClip clip;
        int head = 0;
        const int samplingFrequency = 48000;
        const int lengthSeconds = 1;
        float[] processBuffer = new float[1024];
        float[] microphoneBuffer = new float[lengthSeconds * samplingFrequency];

        void Start() {
            if(Microphone.devices.Length != 0)
            {
                if(deviceIndex > Microphone.devices.Length || deviceIndex < 0)
                {
                    deviceIndex = 0;
                }
                selectedDevice = Microphone.devices[deviceIndex];
                clip = Microphone.Start(selectedDevice, true, lengthSeconds, samplingFrequency);
            }
            else
            {
                Debug.LogError("Device is not found");
            }
        }

        void Update() {
            var position = Microphone.GetPosition(selectedDevice);
            if (position < 0 || head == position) {
                return;
            }

            clip.GetData(microphoneBuffer, 0);
            while (GetDataLength(microphoneBuffer.Length, head, position) > processBuffer.Length) {
                var remain = microphoneBuffer.Length - head;
                if (remain < processBuffer.Length) {
                    for (int i = 0; i < remain; i++) {
                        processBuffer[i] = microphoneBuffer[head + i] * gain;
                    }
                    for (int i = remain; i < processBuffer.Length - remain; i++) {
                        processBuffer[i] = microphoneBuffer[i - remain] * gain;
                    }
                } else {
                    for (int i = 0; i < processBuffer.Length; i++) {
                        processBuffer[i] = microphoneBuffer[head + i] * gain;
                    }
                }

                OVRLipSync.ProcessFrame(Context, processBuffer, Frame, OVRLipSync.AudioDataType.F32_Mono);

                head += processBuffer.Length;
                if (head > microphoneBuffer.Length) {
                    head -= microphoneBuffer.Length;
                }
            }
        }

        public float GetMicVolume() {
            float a = 0;

            foreach (float s in processBuffer) {
                a += Mathf.Abs(s);
            }

            return a / processBuffer.Length;
        }

        static int GetDataLength(int bufferLength, int head, int tail) {
            if (head < tail) {
                return tail - head;
            } else {
                return bufferLength - head + tail;
            }
        }
    }

    #if UNITY_EDITOR

    [CustomEditor(typeof(LowLatencyLipSyncContext))]
    public class LowLatencyContextInspector : Editor {
        LowLatencyLipSyncContext context;

        SerializedProperty indexProperty;

        void OnEnable()
        {
            context = (LowLatencyLipSyncContext)target;

            indexProperty = serializedObject.FindProperty("deviceIndex");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            string[] devices = Microphone.devices;
            int[] deviceIndexes = new int[devices.Length];
            for(int i = 0;i < devices.Length;i++)
            {
                deviceIndexes[i] = i;
            }

            indexProperty.intValue = EditorGUILayout.IntPopup(indexProperty.intValue, devices, deviceIndexes);

            serializedObject.ApplyModifiedProperties();
        }
    }

    #endif
}