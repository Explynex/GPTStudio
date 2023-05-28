using Concentus.Oggfile;
using Concentus.Structs;
using GPTStudio.OpenAI.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Telegram.Bot;
using Telegram.Bot.Types;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Utils;
internal static class VoiceRecognizer
{
    public static async Task<string> RecognizeVoice(Voice voiceFile)
    {
        using var downloadStream = new MemoryStream();
        using var waveStream = new MemoryStream();
        await Env.Client.DownloadFileAsync((await Env.Client.GetFileAsync(voiceFile.FileId).ConfigureAwait(false)).FilePath!, downloadStream);

        using MemoryStream pcmStream = new();
        OpusDecoder decoder = OpusDecoder.Create(48000, 1);
        OpusOggReadStream oggIn = new(decoder, downloadStream);

        while (oggIn.HasNextPacket)
        {
            short[] packet = oggIn.DecodeNextPacket();
            if (packet != null)
            {
                for (int i = 0; i < packet.Length; i++)
                {
                    var bytes = BitConverter.GetBytes(packet[i]);
                    pcmStream.Write(bytes, 0, bytes.Length);
                }
            }
        }
        pcmStream.Position = 0;
        using var wavStream = new RawSourceWaveStream(pcmStream, new WaveFormat(44100, 1));
        WaveFileWriter.WriteWavFileToStream(waveStream, new SampleToWaveProvider16(wavStream.ToSampleProvider()));
        waveStream.Position = 0;
        return await Env.GPTClient.AudioEndpoint.CreateTranscriptionAsync(new AudioTranscriptionRequest(waveStream, "recognized.wav"));
    }
}
