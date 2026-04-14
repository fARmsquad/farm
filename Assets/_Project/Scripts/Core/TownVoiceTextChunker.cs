using System.Collections.Generic;
using System.Text;

namespace FarmSimVR.Core
{
    /// <summary>
    /// Groups streamed text into phrase-sized chunks suitable for TTS.
    /// </summary>
    public sealed class TownVoiceTextChunker
    {
        private const int SoftChunkLength = 48;
        private const int MinimumClauseChunkLength = 24;
        private readonly StringBuilder _buffer = new();

        public IReadOnlyList<string> Append(string delta)
        {
            var completed = new List<string>();
            if (string.IsNullOrEmpty(delta))
                return completed;

            _buffer.Append(delta);
            DrainCompletedChunks(completed);
            return completed;
        }

        public string Flush()
        {
            string remainder = _buffer.ToString().Trim();
            _buffer.Clear();
            return remainder;
        }

        private void DrainCompletedChunks(List<string> completed)
        {
            while (TryTakeNextChunk(out string chunk))
                completed.Add(chunk);
        }

        private bool TryTakeNextChunk(out string chunk)
        {
            chunk = null;
            int boundary = FindSentenceBoundary();
            if (boundary < 0)
                boundary = FindClauseBoundary();

            if (boundary < 0)
                boundary = FindSoftBoundary();

            if (boundary < 0)
                return false;

            chunk = _buffer.ToString(0, boundary + 1).Trim();
            _buffer.Remove(0, boundary + 1);
            TrimLeadingWhitespace();
            return chunk.Length > 0;
        }

        private int FindSentenceBoundary()
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                char current = _buffer[i];
                if (!IsSentenceTerminator(current))
                    continue;

                if (i == _buffer.Length - 1 || char.IsWhiteSpace(_buffer[i + 1]))
                    return i;
            }

            return -1;
        }

        private int FindClauseBoundary()
        {
            if (_buffer.Length < MinimumClauseChunkLength)
                return -1;

            for (int i = _buffer.Length - 1; i >= MinimumClauseChunkLength - 1; i--)
            {
                if (_buffer[i] != ',')
                    continue;

                if (i == _buffer.Length - 1 || char.IsWhiteSpace(_buffer[i + 1]))
                    return i;
            }

            return -1;
        }

        private int FindSoftBoundary()
        {
            if (_buffer.Length < SoftChunkLength)
                return -1;

            for (int i = _buffer.Length - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(_buffer[i]))
                    return i;
            }

            return _buffer.Length - 1;
        }

        private void TrimLeadingWhitespace()
        {
            while (_buffer.Length > 0 && char.IsWhiteSpace(_buffer[0]))
                _buffer.Remove(0, 1);
        }

        private static bool IsSentenceTerminator(char value)
        {
            return value is '.' or '!' or '?' or ';' or ':';
        }
    }
}
