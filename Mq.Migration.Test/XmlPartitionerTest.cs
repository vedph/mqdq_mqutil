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
        public void IsApplicable_NoDiv1_False()
        {
            XDocument doc = TestHelper.LoadResourceDocument("IsApplicable01.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            Assert.False(partitioner.IsApplicable(doc));
        }

        [Fact]
        public void IsApplicable_NoDiv1OfReqType_False()
        {
            XDocument doc = TestHelper.LoadResourceDocument("IsApplicable02.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            Assert.False(partitioner.IsApplicable(doc));
        }

        [Fact]
        public void IsApplicable_Div1OfReqTypeNoOverflow_False()
        {
            XDocument doc = TestHelper.LoadResourceDocument("IsApplicable03.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            Assert.False(partitioner.IsApplicable(doc));
        }

        [Fact]
        public void IsApplicable_Div1OfReqTypeOverflow_True()
        {
            XDocument doc = TestHelper.LoadResourceDocument("IsApplicable04.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            Assert.True(partitioner.IsApplicable(doc));
        }

        [Fact]
        public void IsApplicable_Div1OfReqTypeOverflowDiv2_False()
        {
            XDocument doc = TestHelper.LoadResourceDocument("IsApplicable05.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 10,
                MaxTreshold = 25
            };

            Assert.False(partitioner.IsApplicable(doc));
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
        public void Partition_ApplicableN10M25_4()
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

            AssertExpectedBreaks(breaks, new[] { "13", "27", "37", "43" });
        }

        [Fact]
        public void Partition_ApplicableN5M20_5()
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

            AssertExpectedBreaks(breaks, new[] { "9", "16", "27", "37", "43" });
        }

        [Fact]
        public void Partition_ApplicableFirstEqLast_1()
        {
            XDocument doc = TestHelper.LoadResourceDocument("Sample03.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 5,
                MaxTreshold = 9
            };

            bool touched = partitioner.Partition(doc, "sample");

            Assert.True(touched);

            List<XElement> breaks =
                doc.Descendants(XmlPartitioner.TEI + "pb").ToList();

            AssertExpectedBreaks(breaks, new[] { "10" });
        }

        [Fact]
        public void Partition_ApplicableBreakBeforeMin_2()
        {
            XDocument doc = TestHelper.LoadResourceDocument("Sample04.xml");
            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = 5,
                MaxTreshold = 9
            };

            bool touched = partitioner.Partition(doc, "sample");

            Assert.True(touched);

            List<XElement> breaks =
                doc.Descendants(XmlPartitioner.TEI + "pb").ToList();

            AssertExpectedBreaks(breaks, new[] { "4", "10" });
        }
    }
}
