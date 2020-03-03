using System;
using System.Linq;
using Fastenshtein;

namespace EventDrivenThinking.Tests.Common
{
    public class Statement
    {
        public string[] Words;
        public string SourceStatement;
        public Statement(string statement)
        {
            SourceStatement = statement;
            Words = statement.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(x=>x.ToLowerInvariant())
                .ToArray();
        }

        public double ComputeSimilarity(Statement st)
        {
            double similarity = 0;
            double n = Words.Length;
            foreach (var w in Words)
            {
                int min = st.Words.Min(x => Levenshtein.Distance(w, x));
                if (min <= 2)
                    similarity += (1 / n);
            }

            return similarity;
        }
    }
}