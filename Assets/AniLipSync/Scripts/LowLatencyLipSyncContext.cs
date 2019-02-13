using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XVI.AniLipSync {
    public class LowLatencyLipSyncContext : OVRLipSyncContextBase {
        [Tooltip("Microphone input gain. (Amplitude ratio, no unit.)")]
        [SerializeField] private float gain = 1.0f;

        AudioClip clip;
        int head = 0;
        const int samplingFrequency = 48000;
        const int lengthSeconds = 1;
        float[] processBuffer = new float[1024];
        float[] microphoneBuffer = new float[lengthSeconds * samplingFrequency];

        [SerializeField, HideInInspector]
        string selectedDevice;
        public string SelectedDevice
        {
            get
            {
                return selectedDevice;
            }
            set
            {
                Microphone.End(selectedDevice);
                selectedDevice = value;
                clip = Microphone.Start(selectedDevice, true, lengthSeconds, samplingFrequency);
            }
        }

        void Start() {
            if (selectedDevice != null)
            {
                clip = Microphone.Start(selectedDevice, true, lengthSeconds, samplingFrequency);
            }
            else
            {
                Debug.LogError("マイクデバイスが存在しません");
            }
        }

        void Update() {
            Debug.Log(selectedDevice);

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

        SerializedProperty deviceProperty;

        int deviceIndex = 0;

        void OnEnable()
        {
            context = (LowLatencyLipSyncContext)target;

            deviceProperty = serializedObject.FindProperty("selectedDevice");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            string[] devices = Microphone.devices.Concat(new string[] { "[Default Device]" }).ToArray();
            if (string.IsNullOrEmpty(deviceProperty.stringValue)) {
                deviceIndex = devices.Length - 1;
            } else {
                deviceIndex = Array.IndexOf(devices, deviceProperty.stringValue);
            }

            // '/'はPopupの区切り文字になってしまうため、UnicodeのSlashに置き換えて表示する
            for (var i = 0; i < devices.Length; i++) {
                devices[i] = devices[i].Replace('/', '\u2215');
            }
            deviceIndex = EditorGUILayout.Popup(deviceIndex, devices);

            if (deviceIndex >= devices.Length - 1) {
                deviceIndex = -1;
            }

            // 実行中はSetterを使ってマイク切り替えの処理を呼ぶ
            if(EditorApplication.isPlaying)
            {
                context.SelectedDevice = GetMicrophoneDevice(deviceIndex);
            }
            else
            {
                deviceProperty.stringValue = GetMicrophoneDevice(deviceIndex);
            }

            serializedObject.ApplyModifiedProperties();
        }

        string GetMicrophoneDevice(int deviceIndex)
        {
            if (deviceIndex < 0) return null;
            if (deviceIndex > Microphone.devices.Length) return null;
            if (Microphone.devices.Length == 0) return null;

            return Microphone.devices[deviceIndex];
        }
    }

    #endif
}