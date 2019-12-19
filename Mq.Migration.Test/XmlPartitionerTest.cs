using Mqutil.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Mq.Migration.Test
{
    public class XmlPartitionerTest
    {
        private void AssertExpectedBreaks(IList<XElement> breaks, string[] numbers)
        {
            Assert.Equal(numbers.Length, breaks.Count);
            if (numbers.Length == 0) return;

            XElement div1 = breaks[0].Parent;
            int i = 0;
            foreach (string n in numbers)
            {
                XElement l = div1.Elements(XmlPartitioner.TEI + "l")
                    .First(e => e.Attribute("n").Value == n);
                Assert.Same(l.ElementsAfterSelf().First(), breaks[i++]);
            }
        }

        [Fact]
        public void NotApplicable_TypeFragments_Unchanged()
        {
            XDocument doc = TestHelper.LoadResourceDocument("Sample02.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            bool touched = partitioner.Partition(doc, "sample");

            Assert.False(touched);
            Assert.False(doc.Descendants(XmlPartitioner.TEI + "pb").Any());
        }

        [Fact]
        public void Applicable_N10M25_PartitionedIn3()
        {
            XDocument doc = TestHelper.LoadResourceDocument("Sample01.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            bool touched = partitioner.Partition(doc, "sample");

            Assert.True(touched);

            List<XElement> breaks =
                doc.Descendants(XmlPartitioner.TEI + "pb").ToList();

            AssertExpectedBreaks(breaks, new[] { "13", "37", "43" });
        }

        [Fact]
        public void Applicable_N5M20_PartitionedIn3()
        {
            XDocument doc = TestHelper.LoadResourceDocument("Sample01.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 5,
                MaxTreshold = 20
            };

            bool touched = partitioner.Partition(doc, "sample");

            Assert.True(touched);

            List<XElement> breaks =
                doc.Descendants(XmlPartitioner.TEI + "pb").ToList();

            AssertExpectedBreaks(breaks, new[] { "9", "27", "43" });
        }
    }
}
