using System;
using UnityEngine;

namespace XVI.AniLipSync {
    public class LowLatencyLipSyncContext : OVRLipSyncContextBase {
        [Header("mic gain control")]
        [SerializeField] private float gain = 1.0f;

        AudioClip clip;
        int head = 0;
        const int samplingFrequency = 48000;
        const int lengthSeconds = 1;
        float[] processBuffer = new float[1024];
        float[] microphoneBuffer = new float[lengthSeconds * samplingFrequency];

        void Start() {
            clip = Microphone.Start(null, true, lengthSeconds, samplingFrequency);
        }

        void Update() {
            var position = Microphone.GetPosition(null);
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

                OVRLipSync.ProcessFrame(Context, processBuffer, Frame);

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
}