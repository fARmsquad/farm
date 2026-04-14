using System;
using System.Text;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Encodes interleaved float samples into a PCM16 WAV payload.
    /// </summary>
    public static class TownPcm16WavEncoder
    {
        private const int HeaderSize = 44;
        private const short BitsPerSample = 16;
        private const short PcmAudioFormat = 1;

        public static byte[] Encode(float[] interleavedSamples, int sampleRate, int channels, int frameCount)
        {
            ValidateArguments(interleavedSamples, sampleRate, channels, frameCount);

            int sampleCount = checked(frameCount * channels);
            int pcmByteCount = checked(sampleCount * sizeof(short));
            var wav = new byte[HeaderSize + pcmByteCount];

            WriteHeader(wav, sampleRate, channels, pcmByteCount);
            WriteSamples(wav, interleavedSamples, sampleCount);
            return wav;
        }

        private static void ValidateArguments(float[] interleavedSamples, int sampleRate, int channels, int frameCount)
        {
            if (interleavedSamples == null)
                throw new ArgumentNullException(nameof(interleavedSamples));
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));
            if (frameCount < 0)
                throw new ArgumentOutOfRangeException(nameof(frameCount));

            int requiredSamples = checked(frameCount * channels);
            if (requiredSamples > interleavedSamples.Length)
                throw new ArgumentException("Frame count exceeds the provided interleaved sample buffer.", nameof(frameCount));
        }

        private static void WriteHeader(byte[] wav, int sampleRate, int channels, int pcmByteCount)
        {
            short blockAlign = checked((short)(channels * sizeof(short)));
            int byteRate = checked(sampleRate * blockAlign);

            WriteAscii(wav, 0, "RIFF");
            WriteInt32(wav, 4, 36 + pcmByteCount);
            WriteAscii(wav, 8, "WAVE");
            WriteAscii(wav, 12, "fmt ");
            WriteInt32(wav, 16, 16);
            WriteInt16(wav, 20, PcmAudioFormat);
            WriteInt16(wav, 22, checked((short)channels));
            WriteInt32(wav, 24, sampleRate);
            WriteInt32(wav, 28, byteRate);
            WriteInt16(wav, 32, blockAlign);
            WriteInt16(wav, 34, BitsPerSample);
            WriteAscii(wav, 36, "data");
            WriteInt32(wav, 40, pcmByteCount);
        }

        private static void WriteSamples(byte[] wav, float[] interleavedSamples, int sampleCount)
        {
            int offset = HeaderSize;
            for (int i = 0; i < sampleCount; i++, offset += sizeof(short))
                WriteInt16(wav, offset, ConvertSample(interleavedSamples[i]));
        }

        private static short ConvertSample(float sample)
        {
            float clamped = Clamp(sample, -1f, 1f);
            if (clamped <= -1f)
                return short.MinValue;

            return (short)Math.Round(clamped * short.MaxValue, MidpointRounding.AwayFromZero);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static void WriteAscii(byte[] target, int offset, string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, target, offset, bytes.Length);
        }

        private static void WriteInt16(byte[] target, int offset, short value)
        {
            target[offset] = (byte)(value & 0xff);
            target[offset + 1] = (byte)((value >> 8) & 0xff);
        }

        private static void WriteInt32(byte[] target, int offset, int value)
        {
            target[offset] = (byte)(value & 0xff);
            target[offset + 1] = (byte)((value >> 8) & 0xff);
            target[offset + 2] = (byte)((value >> 16) & 0xff);
            target[offset + 3] = (byte)((value >> 24) & 0xff);
        }
    }
}
