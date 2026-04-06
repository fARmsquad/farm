using System;
using NUnit.Framework;
using FarmSimVR.Core.Farming;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class CropGrowthCalculatorTests
    {
        private CropGrowthCalculator _calculator;
        private CropData _defaultCrop;

        [SetUp]
        public void SetUp()
        {
            _calculator = new CropGrowthCalculator();
            _defaultCrop = new CropData(baseGrowthRate: 10f, maxGrowth: 100f);
        }

        // === Basic Growth ===

        [Test]
        public void CalculateGrowth_SunnyNormalSoilNormalTemp_ReturnsBaseGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(10f, result.GrowthAmount, 0.001f);
            Assert.IsFalse(result.IsFullyGrown);
        }

        [Test]
        public void CalculateGrowth_DeltaTimeScalesGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 2f);

            Assert.AreEqual(20f, result.GrowthAmount, 0.001f);
        }

        // === Rain Weather ===

        [Test]
        public void CalculateGrowth_RainWeather_DoublesGrowthRate()
        {
            var conditions = new GrowthConditions(WeatherType.Rain, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(20f, result.GrowthAmount, 0.001f);
        }

        // === Temperature ===

        [Test]
        public void CalculateGrowth_TemperatureBelow10_HalvesGrowthRate()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 5f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(5f, result.GrowthAmount, 0.001f);
        }

        [Test]
        public void CalculateGrowth_TemperatureAbove35_HalvesGrowthRate()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 40f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(5f, result.GrowthAmount, 0.001f);
        }

        [Test]
        public void CalculateGrowth_TemperatureExactly10_NormalGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 10f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(10f, result.GrowthAmount, 0.001f);
        }

        [Test]
        public void CalculateGrowth_TemperatureExactly35_NormalGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 35f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(10f, result.GrowthAmount, 0.001f);
        }

        // === Soil Quality ===

        [Test]
        public void CalculateGrowth_PoorSoil_MultipliesBy05()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Poor);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(5f, result.GrowthAmount, 0.001f);
        }

        [Test]
        public void CalculateGrowth_RichSoil_MultipliesBy15()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Rich);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(15f, result.GrowthAmount, 0.001f);
        }

        // === Combined Conditions ===

        [Test]
        public void CalculateGrowth_RainAndRichSoil_StacksMultiplicatively()
        {
            var conditions = new GrowthConditions(WeatherType.Rain, 20f, SoilQuality.Rich);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            // base(10) * rain(2.0) * rich(1.5) = 30
            Assert.AreEqual(30f, result.GrowthAmount, 0.001f);
        }

        [Test]
        public void CalculateGrowth_RainAndColdTemp_StacksMultiplicatively()
        {
            var conditions = new GrowthConditions(WeatherType.Rain, 5f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            // base(10) * rain(2.0) * cold(0.5) = 10
            Assert.AreEqual(10f, result.GrowthAmount, 0.001f);
        }

        // === Growth Cap ===

        [Test]
        public void CalculateGrowth_GrowthCannotExceedMaxGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 95f, deltaTime: 1f);

            Assert.AreEqual(5f, result.GrowthAmount, 0.001f);
            Assert.IsTrue(result.IsFullyGrown);
        }

        [Test]
        public void CalculateGrowth_AlreadyAtMaxGrowth_ReturnsZeroGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 100f, deltaTime: 1f);

            Assert.AreEqual(0f, result.GrowthAmount, 0.001f);
            Assert.IsTrue(result.IsFullyGrown);
        }

        // === Edge Cases ===

        [Test]
        public void CalculateGrowth_ZeroDeltaTime_ReturnsZeroGrowth()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 0f);

            Assert.AreEqual(0f, result.GrowthAmount, 0.001f);
            Assert.IsFalse(result.IsFullyGrown);
        }

        [Test]
        public void CalculateGrowth_NegativeDeltaTime_ThrowsArgumentException()
        {
            var conditions = new GrowthConditions(WeatherType.Sunny, 20f, SoilQuality.Normal);

            Assert.Throws<ArgumentException>(() =>
                _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: -1f));
        }

        [Test]
        public void CalculateGrowth_CloudyWeather_NormalGrowthRate()
        {
            var conditions = new GrowthConditions(WeatherType.Cloudy, 20f, SoilQuality.Normal);

            var result = _calculator.CalculateGrowth(_defaultCrop, conditions, currentGrowth: 0f, deltaTime: 1f);

            Assert.AreEqual(10f, result.GrowthAmount, 0.001f);
        }
    }
}
