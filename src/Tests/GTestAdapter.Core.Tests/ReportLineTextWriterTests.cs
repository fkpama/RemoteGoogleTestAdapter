using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleTestAdapter.Remote.Execution;

namespace GTestAdapter.Core.Tests
{
    internal class ReportLineTextWriterTests
    {
        int callCount;
        List<string> lines = new();
        ReportLineTextWriter sut;
        [SetUp]
        public void SetUp()
        {
            sut = new(report);
            lines.Clear();
            callCount = 0;
        }

        private void report(string line)
        {
            callCount++;
            lines.Add(line);
        }

        [Test]
        public void can_add_multiple_lines()
        {
            sut.Write("Hello ");
            sut.Write("World\n");

            callCount.Should().Be(1);
            lines[0].Should().Be("Hello World");
        }

        [Test]
        public void can_add_multiple_lines_in_a_single_call()
        {
            var sut = new ReportLineTextWriter(report);
            sut.Write("Hello\nWorld\n");
            callCount.Should().Be(2);
            lines[0].Should().Be("Hello");
            lines[1].Should().Be("World");
        }

        [Test]
        public void can_buffers_last_line()
        {
            var sut = new ReportLineTextWriter(report);
            sut.Write("Hello\nWorld");

            callCount.Should().Be(1);
            lines.First().Should().Be("Hello");
            sut.CurrentLine.Should().Be("World");

            sut.Write("Foo\n");
            callCount.Should().Be(2);
            lines.Last().Should().Be("WorldFoo");
        }
    }
}
