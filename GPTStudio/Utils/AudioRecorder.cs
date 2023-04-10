using NAudio.Utils;
using NAudio.Wave;
using System;
using System.IO;

namespace GPTStudio.Utils
{
    class AudioRecorder : IDisposable
    {
        private readonly WaveFileWriter waveFile;
        public MemoryStream MemoryStream { get;} 
        private readonly WaveIn waveSource;
        public bool IsRecording { get; private set; } = false;

        public AudioRecorder()
        {
            waveSource = new WaveIn
            {
                WaveFormat = new WaveFormat(44100, 1),
            };

            MemoryStream = new MemoryStream();
            waveFile     = new WaveFileWriter(new IgnoreDisposeStream(MemoryStream), waveSource.WaveFormat);
            waveSource.RecordingStopped += OnRecordingStopped;
            waveSource.DataAvailable += OnDataAvailable;
        }

        public void Start()
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Audio recording stream is already running");
            }

            IsRecording = true;
            waveSource.StartRecording();
        }

        public void Stop()
        {
            if (!IsRecording) return;
            waveSource.StopRecording();
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if(waveFile != null) 
            {
                waveSource.StopRecording();
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();
            }
        }

        public void Dispose()
        {
            MemoryStream.Dispose();
            waveSource.Dispose();
        }
    }
}
