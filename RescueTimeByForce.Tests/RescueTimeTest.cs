using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using RescueTimeByForce;

namespace RescueTimeByForce.Tests
{
    public class TestFact
    {
        [Fact]
        public void Scratch()
        {
            var fixture = DateTime.Parse("2011-04-12T00:05:00");
            Assert.Equal(2011, fixture.Year);
            Assert.Equal(4, fixture.Month);
            Assert.Equal(0, fixture.Hour);
            Assert.Equal(5, fixture.Minute);
        }

        [Fact]
        public void SplitLinesInBufferTest()
        {
            string input = "Hello World\nTest\n\nOther Stuff\n\n";
            string[] expected = new[] { "Hello World", "Test", "Other Stuff" };

            var output = ActivityRecord.splitLinesInBuffer(input).ToArray();

            Assert.Equal(expected.Length, output.Length);
            expected.Zip(output, (e, a) => new { e, a }).Run(x => Assert.Equal(x.e, x.a));
        }

        [Fact]
        public void FetchRescueTimeDataTest()
        {
            var fixture = ActivityRecord.FetchRescueTimeData("B633MZSjMrwOV7N8lueArTYcauGGkYMUEIVgPCGW").First();
            Assert.True(fixture.Length > 4);
        }

        [Fact]
        public void TryCalculatingAggregateTimes()
        {
            var fixture = ActivityRecord.FetchRescueTimeData("B633MZSjMrwOV7N8lueArTYcauGGkYMUEIVgPCGW").First();

            var lookup = fixture.ToLookup(x => x.Time);
            var debugCalc = EnumerableEx.Generate(fixture.Min(x => x.Time), x => x <= fixture.Max(y => y.Time), x => x.AddMinutes(5.0), x => x)
                .Select(x => new { Key = x, Sum = lookup[x].Sum(y => y.ProductivityInSeconds), Len = lookup[x].Count()})
                .Scan0(new { Time = 0.0, Date = DateTime.MinValue } , (acc, x) => {
                    var toAdd = x.Sum;
                    if (x.Len == 0) {
                        toAdd = 60.0 * (acc.Time > 0 ? -1.0 : 1.0);
                    }
                    var secs = (acc.Time + toAdd).Clamp(-10 * 60.0, 5 * 60.0);
                    if (secs <= -10 * 60.0 && x.Sum != 0.0) {
                        secs += 5 * 60.0;
                    }
                    return new {Time = secs, Date = x.Key};
                }).ToList();

            /*
            var debugCalc = fixture
                .ToLookup(x => x.Time)
                .Select(x => new {x.Key, Sum = x.Sum(y => y.ProductivityInSeconds)})
                .Scan0(new { Time = 0.0, Date = DateTime.MinValue } , (acc, x) => {
                    var secs = (acc.Time + x.Sum).Clamp(-10 * 60.0, 5 * 60.0);
                    if (secs <= -10 * 60.0 && x.Sum != 0.0) {
                        secs += 5 * 60.0;
                    }
                    return new {Time = secs, Date = x.Key};
                }).ToList();
             */

            var series = EnumerableEx.Generate(fixture.Min(x => x.Time), x => x <= fixture.Max(y => y.Time), x => x.AddMinutes(5.0), x => x)
                .Select(x => new { Key = x, Sum = lookup[x].Sum(y => y.ProductivityInSeconds), Len = lookup[x].Count()})
                .Aggregate(new {List = new List<DateTime>(), Secs = 0.0}, (acc, x) => {
                    var toAdd = x.Sum;
                    if (x.Len == 0) {
                        toAdd = 60.0 * (acc.Secs > 0 ? -1.0 : 1.0);
                    }
                    var secs = (acc.Secs + toAdd).Clamp(-10 * 60.0, 5 * 60.0);
                    if (secs <= -10 * 60.0 && x.Sum != 0.0) {
                        acc.List.Add(x.Key);
                        secs += 5 * 60.0;
                    }

                    return new {acc.List, Secs = secs};
                }).List;

            Assert.True(false);
        }
    }
}
