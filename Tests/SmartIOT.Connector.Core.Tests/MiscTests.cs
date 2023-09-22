using SmartIOT.Connector.Core.Util;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SmartIOT.Connector.Core.Tests
{
    public class MiscTests
    {
        [Fact]
        public void Test_ConnectionStringParser()
        {
            var d = ConnectionStringParser.ParseTokens("test://  Uno = 1  ; Due = 2;;; Null =  ; ;;;;");

            Assert.Equal(3, d.Count);
            Assert.Equal("1", d["uno"]);
            Assert.Equal("2", d["due"]);
            Assert.Equal(string.Empty, d["null"]);
            Assert.False(d.ContainsKey("Null"));
            Assert.False(d.ContainsKey(""));
        }

        [Fact]
        public void Test_ConnectionStringParser_invalid_connection()
        {
            var d = ConnectionStringParser.ParseTokens("invalid : / /");

            Assert.Empty(d);
        }

        [Fact]
        public void Test_manualResetEvent_wait()
        {
            var e = new ManualResetEventSlim();

            e.Set();

            e.Wait();

            Assert.True(e.Wait(TimeSpan.FromSeconds(1)));
            Assert.True(e.Wait(TimeSpan.FromSeconds(1)));
            Assert.True(e.Wait(TimeSpan.FromSeconds(1)));

            e.Reset();

            Assert.False(e.Wait(TimeSpan.FromMilliseconds(100)));
            Assert.False(e.Wait(TimeSpan.FromMilliseconds(100)));
            Assert.False(e.Wait(TimeSpan.FromMilliseconds(100)));
        }

        [Fact]
        public void Test_regex()
        {
            Regex r = new Regex(@"^DB(?<tag>[0-9]+)$");
            Assert.Matches(r, "DB20");

            Assert.False(r.Match("aDB20").Success);
            Assert.False(r.Match("DB").Success);
            Assert.True(r.Match("DB20").Success);
            Assert.Equal("20", r.Match("DB20").Groups["tag"].Value);
        }
    }
}
