using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.Infrastructure.Azure
{
    internal class SpeechHandler : IDisposable
    {
        public bool IsSpeaking { get; private set; }
        private bool IsStopRequested;

        private readonly string OAuthTokenEndpoint;
        private readonly string CognetiveServiceEndpoint;
        private readonly string VoicesListEndpoint;
        private readonly HttpClient Client;
        public Queue<string> TextToSpeechQueue { get; private set; } = new();

        /// <summary>
        /// Set token for connection with OAuth, use GetOAuthToken() to get or load from cache
        /// </summary>
      //  public string OAuthToken { get; set; }

        public SpeechHandler(string subscriptionKey, string region)
        {
            Client = new();
            OAuthTokenEndpoint = $"https://{region}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
            VoicesListEndpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/voices/list";
            CognetiveServiceEndpoint = $"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1";

            Client.DefaultRequestHeaders.Add("User-Agent", "GPTStudio-OpenAI");
            Client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            Client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");
        }



        public void Dispose() => Client.Dispose();
        public async Task StartSpeaking(string text, string speecherName, Action<HttpStatusCode> completionEvent = null)
        {
            if (IsSpeaking) 
                return;

            #region HTTP
            var content = new StringBuilder("<speak version='1.0' xml:lang='eu-US'><voice xml:lang='eu-US' xml:gender='Male' name='")
            .Append(speecherName).Append("'>").Append(text).Append("</voice></speak>");
            using var request = new HttpRequestMessage(HttpMethod.Post, CognetiveServiceEndpoint)
            {
                Content = new StringContent(content.ToString(), Encoding.UTF8, "application/ssml+xml"),
            };

            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if(response.StatusCode != HttpStatusCode.OK)
            {
                completionEvent?.Invoke(response.StatusCode);
                return;
            }
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
            completionEvent?.Invoke(HttpStatusCode.OK);
            IsSpeaking = false;

            static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
            {
                WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                    frame.FrameLength, frame.BitRate);
                return new AcmMp3FrameDecompressor(waveFormat);
            }

        }

        public async void StartQueueSpeaking(string speecherName)
        {
            while(TextToSpeechQueue.Count != 0 && !IsStopRequested)
            {
                await StartSpeaking(TextToSpeechQueue.Dequeue(), speecherName);
            }
        }

        public async Task StopSpeaking()
        {
            IsStopRequested = true;
            await Task.Delay(20);
            while(IsSpeaking)
            {
                await Task.Delay(180);
            }
        }

        public async Task GetVoicesListAsync()
        {
            var result = await Client.GetAsync(VoicesListEndpoint);
        }

        public async Task<string> GetOAuthToken()
        {
            using var request = new HttpRequestMessage();
            using var result = await Client.PostAsync(OAuthTokenEndpoint, null);
            if (result.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException();
            return await result.Content.ReadAsStringAsync();
        }

    }
}
