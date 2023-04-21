using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.Infrastructure.Azure;

internal class SpeechHandler : IDisposable
{
    public bool IsSpeaking { get; private set; }
    public Action<ESpeechResponse> CompletionEvent { get; set; }
    public Queue<string> TextToSpeechQueue { get; private set; } = new();


    private bool IsStopRequested;
    private readonly string OAuthTokenEndpoint;
    private readonly string CognetiveServiceEndpoint;
    private readonly string VoicesListEndpoint;
    private readonly HttpClient Client;
    private readonly byte[] buffer = new byte[8192];

    
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
    private async Task<ESpeechResponse> OpenMp3SpeechStream(string text,string speecherName)
    {
        #region HTTP Request
        var content = new StringBuilder("<speak version='1.0' xml:lang='eu-US'><voice xml:lang='eu-US' xml:gender='Male' name='")
        .Append(speecherName).Append("'>").Append(text).Append("</voice></speak>");
        using var request = new HttpRequestMessage(HttpMethod.Post, CognetiveServiceEndpoint)
        {
            Content = new StringContent(content.ToString(), Encoding.UTF8, "application/ssml+xml"),
        };

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return ESpeechResponse.InvalidSubscriptionKey;
            if (response.ReasonPhrase.EndsWith("supported neural voice names in chosen languages."))
                return ESpeechResponse.InvalidSpeecherName;
                
            return ESpeechResponse.BadRequest;
        }

        content.Clear();
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        #endregion

        Mp3Frame frame;
        IsStopRequested = false;
        IMp3FrameDecompressor decompressor = null;
        BufferedWaveProvider bufferedWaveProvider = null;
        using var waveOut = new WaveOut();

        using var readFullyStream = new ReadFullyStream(stream);
        do
        {
            if (IsStopRequested)
                break;
            
            try
            {
                frame = Mp3Frame.LoadFromStream(readFullyStream);
            }
            catch
            {
                break;
            }

            if (frame == null)
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
                await Task.Delay(200).ConfigureAwait(false);


            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
        } while (true);

        //Waiting end because stream reading ends before playback
        while (bufferedWaveProvider.BufferedBytes != 0)
        {
            if (IsStopRequested)
            {
                waveOut.Stop();
                bufferedWaveProvider.ClearBuffer();
            }

            await Task.Delay(200).ConfigureAwait(false);
        }

        decompressor?.Dispose();

        return IsStopRequested ? ESpeechResponse.SpeechingCanceled : ESpeechResponse.OK;

        static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }
    }

    public async Task StartSpeaking(string text, string speecherName)
    {
        if(IsSpeaking)
        {
            CompletionEvent?.Invoke(ESpeechResponse.AlreadySpeaking);
            return;
        }

        IsSpeaking = true;

        var status = await OpenMp3SpeechStream(text, speecherName).ConfigureAwait(false);

        CompletionEvent?.Invoke(status);
        IsSpeaking = false;
    }

    /// <summary>
    ///  Plays chunks of text from the TextToSpeechQueue queue. Suitable for data that is not received entirely, for example, reading by sentences. To stop, call StopSpeaking()
    /// </summary>
    /// <param name="speecherName">Speecher name and locale from https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support</param>
    /// <param name="waitingParams">If the text does not enter the queue for some time, set the wait parameter (timeout in ms, timeout cycles) 
    /// StopRequest is checked with each new cycle after cooldown  </param>
    public async void StartQueueSpeaking(string speecherName,(int,int) waitingParams = default)
    {
        IsSpeaking = true;
        int currentWaitingCycle = waitingParams.Item2;
        ESpeechResponse result = ESpeechResponse.OK;

        while (!IsStopRequested)
        {
            if (TextToSpeechQueue.Count != 0)
            {
                StringBuilder msgBuilder = default;
                if (TextToSpeechQueue.Count > 1)
                {
                    msgBuilder = new();
                    for (int i = 0; i < TextToSpeechQueue.Count; i++)
                        msgBuilder.Append(TextToSpeechQueue.Dequeue());
                }
                else if (TextToSpeechQueue.Peek().Length <= 1) continue;
                
                result = await OpenMp3SpeechStream(msgBuilder == null ? TextToSpeechQueue.Dequeue() : msgBuilder.ToString(), speecherName);
                if (result != ESpeechResponse.OK)
                    break;
                currentWaitingCycle = waitingParams.Item2;
            }
            else if (waitingParams != default && currentWaitingCycle > 0)
            {
                await Task.Delay(waitingParams.Item1);
                currentWaitingCycle--;
            }
            else break;
        }

        CompletionEvent?.Invoke(result);
        IsSpeaking = false;
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

    public enum ESpeechResponse
    {
        OK,
        NoInternetConnection,
        AlreadySpeaking,
        SpeechingCanceled,
        InvalidSpeecherName,
        BadRequest,
        InvalidSubscriptionKey
    }

}
