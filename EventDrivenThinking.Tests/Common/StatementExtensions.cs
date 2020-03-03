using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using FluentAssertions.Execution;

namespace EventDrivenThinking.Tests.Common
{
    public static class AssertionExtensions
    {
        public static void BeEquivalentTo(this object src, object dst)
        {
            if (src == dst) return;
            if (src == null && dst == null) return;
            
            if(dst.GetType() != src.GetType())
                throw new AssertionFailedException($"Expected to check type of {dst.GetType().Name} but actual type is {src.GetType().Name}");

            var mth = typeof(AssertionExtensions)
                .GetMethod(nameof(BeEquivalentToCore), BindingFlags.NonPublic | BindingFlags.Static);
            mth.MakeGenericMethod(src.GetType()).Invoke(null, new object[]{src, dst});
         }

        private static void BeEquivalentToCore<T>(this T src, T dst)
        {
            src.Should().BeEquivalentTo(dst);
        }
    }

    public static class StatementExtensions
    {

        public static string FindSimilar(this string statement, IEnumerable<string> statements)
        {
            Statement s = new Statement(statement);
            var array = statements.Select(x => new Statement(x)).OrderByDescending(y => s.ComputeSimilarity(y)).ToArray();
            return array[0].SourceStatement;
        }
    }
}