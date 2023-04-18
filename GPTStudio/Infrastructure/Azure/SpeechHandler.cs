using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GPTStudio.Infrastructure.Azure
{
    internal class SpeechHandler
    {
        public bool IsSpeaking { get; private set; }
        private bool IsStopRequested;

        private Action completionAction;
        private string OAuthTokenEndpoint;
        private string CognetiveServiceEndpoint;
        private string VoicesListEndpoint;
        private HttpClient Client;
        public string Token { get; private set; }

        public SpeechHandler(string subscriptionKey, string region)
        {
            Client = new();
            OAuthTokenEndpoint = $"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
            VoicesListEndpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/voices/list";
            CognetiveServiceEndpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";
            // Token                                      = "eyJhbGciOiJFUzI1NiIsImtpZCI6ImtleTEiLCJ0eXAiOiJKV1QifQ.eyJyZWdpb24iOiJub3J0aGV1cm9wZSIsInN1YnNjcmlwdGlvbi1pZCI6ImRiNTg4ZWIxN2ZjYzQ2NTdhZTA2MjMzZTVhNWU2OTUyIiwicHJvZHVjdC1pZCI6IlNwZWVjaFNlcnZpY2VzLkYwIiwiY29nbml0aXZlLXNlcnZpY2VzLWVuZHBvaW50IjoiaHR0cHM6Ly9hcGkuY29nbml0aXZlLm1pY3Jvc29mdC5jb20vaW50ZXJuYWwvdjEuMC8iLCJhenVyZS1yZXNvdXJjZS1pZCI6Ii9zdWJzY3JpcHRpb25zL2FjYWI3Mjg4LTc2ZWEtNDU0Mi1hMzdlLTFlN2JmM2YxZjA5MC9yZXNvdXJjZUdyb3Vwcy9FeHBseW5lL3Byb3ZpZGVycy9NaWNyb3NvZnQuQ29nbml0aXZlU2VydmljZXMvYWNjb3VudHMvZXhwbHluZSIsInNjb3BlIjoic3BlZWNoc2VydmljZXMiLCJhdWQiOiJ1cm46bXMuc3BlZWNoc2VydmljZXMubm9ydGhldXJvcGUiLCJleHAiOjE2ODE3MjQ2MjIsImlzcyI6InVybjptcy5jb2duaXRpdmVzZXJ2aWNlcyJ9.E1gsCYwhvVhGDrEDcwxoCZ2zQRxE7IhudL76ONFn9QlztA3fgkMPVYRw3kdBnrkUAr1L1JgZ4a2T2qS9WZUE1g";

            Client.DefaultRequestHeaders.Add("User-Agent", "GPTStudio-OpenAI");
            Client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
           // Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }


        

        public async void SpeakAsync(string text, string speecherName, Action completionEvent = null)
        {
            #region HTTP
            var content = new StringBuilder("<speak version='1.0' xml:lang='eu-US'><voice xml:lang='eu-US' xml:gender='Male' name='")
            .Append(speecherName).Append("'>").Append(text).Append("</voice></speak>");
            using var request = new HttpRequestMessage(HttpMethod.Post, CognetiveServiceEndpoint)
            {
                Content = new StringContent(content.ToString(), Encoding.UTF8, "application/ssml+xml"),
            };
            request.Headers.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            request.Dispose();
            content.Clear();
            using var stream = await response.Content.ReadAsStreamAsync();
            #endregion

            Mp3Frame frame;
            IsSpeaking                                = true;
            IsStopRequested                           = false;
            IMp3FrameDecompressor decompressor        = null;
            BufferedWaveProvider bufferedWaveProvider = null;
            var waveOut                               = new WaveOut();
            var buffer                                = new byte[8192];

            using var readFullyStream = new ReadFullyStream(stream);
            do
            {
                if(IsStopRequested)
                {
                    break;
                }
                try
                {
                    frame = Mp3Frame.LoadFromStream(readFullyStream);
                }
                catch (EndOfStreamException)
                {
                    break;
                }
                catch
                {
                    break;
                }

                if (frame == null || IsStopRequested)
                    break;

                if (decompressor == null)
                {
                    decompressor = CreateFrameDecompressor(frame);
                    bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat) { BufferDuration = TimeSpan.FromSeconds(4) };
                    waveOut.Init(bufferedWaveProvider);
                    waveOut.Play();
                }

                int decompressed = decompressor.DecompressFrame(frame, buffer, 0);

                //Waiting for playback as buffering will cause an overflow
                while (bufferedWaveProvider.BufferedDuration.Seconds > 2)
                    await Task.Delay(200);
                

                bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
            } while (true);

            //Waiting end because stream reading ends before playback
            while (bufferedWaveProvider.BufferedBytes != 0)
            {
                if(IsStopRequested)
                {
                    waveOut.Stop();
                    bufferedWaveProvider.ClearBuffer();
                }

                await Task.Delay(200);
            }

            
            decompressor?.Dispose();
            waveOut?.Dispose();
            IsSpeaking = false;
            completionEvent?.Invoke();

            static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
            {
                WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                    frame.FrameLength, frame.BitRate);
                return new AcmMp3FrameDecompressor(waveFormat);
            }

        }

        public void StopSpeaking() => IsStopRequested = true;

        public async Task GetVoicesListAsync()
        {
            var result = await Client.GetAsync(VoicesListEndpoint);
        }

        public async Task GetOAuthToken()
        {
            if (Token != null)
                return;

            var result = await Client.PostAsync(OAuthTokenEndpoint, null);
            Token = await result.Content.ReadAsStringAsync();
        }

    }
}
