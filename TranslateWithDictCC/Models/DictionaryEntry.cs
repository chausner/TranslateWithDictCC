using System;

namespace TranslateWithDictCC.Models
{
    class DictionaryEntry
    {
        public string Word1 { get; set; }

        public string Word2 { get; set; }

        public string WordClasses { get; set; }

        public TextSpan[] MatchSpans { get; set; }
    }

    struct TextSpan
    {
        public int Offset { get; }
        public int Length { get; }

        public TextSpan(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }

        public bool Intersects(TextSpan span)
        {
            return Math.Max(Offset, span.Offset) < Math.Min(Offset + Length, span.Offset + span.Length);
        }

        public bool Contains(TextSpan span)
        {
            return Offset <= span.Offset && Offset + Length >= span.Offset + span.Length;
        }
    }
}
