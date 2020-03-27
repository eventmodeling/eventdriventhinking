using System;
using System.Linq;
using EventDrivenThinking.Utils;
using FluentAssertions;
using Xunit;

namespace EventDrivenThinking.Tests
{
    public class SynchronizedCollectionTests
    {
        [Fact]
        public void ToArrayReturnsAllItems()
        {
            SynchronizedCollection<int> collection = new SynchronizedCollection<int>();

            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            var array = collection.ToArray();

            array.Should().BeEquivalentTo(1, 2, 3);
        }
    }
}