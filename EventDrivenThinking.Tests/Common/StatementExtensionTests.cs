using FluentAssertions;
using Humanizer;
using Xunit;

namespace EventDrivenThinking.Tests.Common
{
    public class StatementExtensionTests
    {
        [Fact]
        public void CheckSimilarity()
        {
            string ev1 = "Board block was created";
            string ev2 = "BoardBlocksLinked".Humanize();
            string ev3 = "Board blocks will be linked";

            var result = ev3.FindSimilar(new[] {ev1, ev2});

            result.Should().Be(ev2);
        }
    }
}