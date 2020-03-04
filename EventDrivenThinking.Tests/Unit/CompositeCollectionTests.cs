using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using EventDrivenThinking.Ui;
using FluentAssertions;
using Xunit;

namespace EventDrivenThinking.Tests.Unit
{
    public class CompositeCollectionTests
    {
        [Fact]
        public void SortInsertOnRightSpot()
        {
            CompositeCollection<int> collection = new CompositeCollection<int>(true);

            
            ViewModelCollection<int> a = new ViewModelCollection<int>(new List<int>());
            ViewModelCollection<int> b = new ViewModelCollection<int>(new List<int>());

            collection.Add(a);
            collection.Add(b);

            Random r = new Random();
            for (int i = 0; i < 200; i++)
            {
                if ((i % 2) == 1)
                {
                    a.Add(r.Next());
                }
                else b.Add(r.Next());
            }

            int prv = collection[0];
            for (int i = 1; i < collection.Count; i++)
            {
                if (collection[i] < prv)
                    throw new Exception($"{i}");
                else prv = collection[i];
            }
        }
    }
}
