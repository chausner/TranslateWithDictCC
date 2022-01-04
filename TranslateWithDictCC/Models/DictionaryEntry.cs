using System;

namespace TranslateWithDictCC.Models
{
    record DictionaryEntry
    {
        public string Word1 { get; init; }
        public string Word2 { get; init; }
        public string WordClasses { get; init; }
        public TextSpan[] MatchSpans { get; init; }
    }

    record struct TextSpan(int Offset, int Length)
    { 
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
