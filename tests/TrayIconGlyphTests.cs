// Copyright (c) 2026 Giacomo Lacava
// Licensed under the MIT License. See LICENSE in the project root for license information.

using BTChargeInTray;
using Xunit;

namespace BtBatteryTray.Tests
{
    public class TrayIconGlyphTests
    {
        [Theory]
        [InlineData(24, "2")]
        [InlineData(10, "1")]
        [InlineData(19, "1")]
        [InlineData(20, "2")]
        [InlineData(59, "5")]
        [InlineData(60, "6")]
        [InlineData(87, "8")]
        [InlineData(99, "9")]
        public void TwoDigitLevel_ShowsFirstDigit(int percent, string expected)
        {
            Assert.Equal(expected, TrayIconRenderer.Glyph(percent));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(9)]
        public void SingleDigitLevel_ShowsZero(int percent)
        {
            Assert.Equal("0", TrayIconRenderer.Glyph(percent));
        }

        [Theory]
        [InlineData(100)]
        [InlineData(101)]
        public void FullCharge_ShowsF(int percent)
        {
            Assert.Equal("F", TrayIconRenderer.Glyph(percent));
        }

        [Fact]
        public void Offline_ShowsDash()
        {
            Assert.Equal("–", TrayIconRenderer.Glyph(null));
        }

        [Fact]
        public void NegativeLevel_ClampsToZero()
        {
            Assert.Equal("0", TrayIconRenderer.Glyph(-3));
        }
    }
}
