using System;
using System.Text;
using Febucci.Numbers;

namespace Febucci.TextUtils.Parsing
{
    public sealed class PlainTagParser : TagParserBase
    {
        readonly string tag;

        public PlainTagParser(string tag, char startSymbol, char closingSymbol, char endSymbol) : base(startSymbol, closingSymbol,
            endSymbol)
        {
            this.tag = tag;
            results = Array.Empty<Vector2Int>();
        }

        bool hasOpened;
        public Vector2Int[] results;


        //--- METHODS ---
        public override bool TryProcessingTag(string textInsideBrackets, int tagLength, ref int realTextIndex,
            StringBuilder finalTextBuilder, int internalOrder)
        {
            textInsideBrackets = textInsideBrackets.ToLower();
            if (tagLength <= 1) return false;

            if (textInsideBrackets[0] == closingSymbol) // closes
            {
                if (!textInsideBrackets.Substring(1, tagLength - 1).Equals(tag)) return false;

                if (results.Length > 0 && hasOpened)
                {
                    results[results.Length - 1].y = realTextIndex;
                    hasOpened = true;
                    return true;
                }
            }
            else
            {
                if (!textInsideBrackets.Equals(tag)) return false;
                hasOpened = true;
                var newTag = new Vector2Int(realTextIndex, int.MaxValue);
                Array.Resize(ref results, results.Length+1);
                results[results.Length-1] = newTag;
                return true;
            }

            return false;
        }
    }
}