/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class CovarianceTests : CommonIndicatorTests<IBaseDataBar>
    {
        private static readonly Symbol _spy = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
        private static readonly Symbol _qqq = Symbol.Create("QQQ", SecurityType.Equity, Market.USA);

        protected override string TestFileName => "spy_qqq_cov.csv";
        protected override string TestColumnName => "covariance";

        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new Covariance("Covariance_Test", _spy, _qqq, 252);
        }

        protected override List<Symbol> GetSymbols()
        {
            return new List<Symbol> { _spy, _qqq };
        }



        [Test]
        public override void ComparesAgainstExternalDataAfterReset()
        {
            var indicator = CreateIndicator();
            indicator.Reset();
            ComparesAgainstExternalData();
        }

        [Test]
        public override void WarmsUpProperly()
        {
            int period = 10;
            var indicator = new Covariance("test", _spy, _qqq, period);
            var start = new DateTime(2020, 1, 1);

            for (int i = 0; i < period + 1; i++)
            {
                indicator.Update(new TradeBar(start.AddDays(i), _spy, 100 + i, 100 + i, 100 + i, 100 + i, 1000));
                indicator.Update(new TradeBar(start.AddDays(i), _qqq, 100 + i, 100 + i, 100 + i, 100 + i, 1000));
            }

            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public override void ResetsProperly()
        {
            var indicator = CreateIndicator();
            var start = new DateTime(2020, 1, 1);

            for (int i = 0; i < 260; i++)
            {
                indicator.Update(new TradeBar(start.AddDays(i), _spy, 100 + i, 100 + i, 100 + i, 100 + i, 1000));
                indicator.Update(new TradeBar(start.AddDays(i), _qqq, 200 + i, 200 + i, 200 + i, 200 + i, 1000));
            }

            Assert.IsTrue(indicator.IsReady);

            indicator.Reset();

            Assert.IsFalse(indicator.IsReady);
            Assert.AreEqual(0m, indicator.Current.Value);
        }

        [Test]
        public override void AcceptsRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var start = new DateTime(2020, 1, 1);

            for (int i = 0; i < 10; i++)
            {
                var time = start.AddDays(i);
                Assert.DoesNotThrow(() => indicator.Update(
                    new RenkoBar(_spy, time, time, 100 + i, 101 + i, 99 + i, 100 + i, 1000)));
                Assert.DoesNotThrow(() => indicator.Update(
                    new RenkoBar(_qqq, time, time, 100 + i, 101 + i, 99 + i, 100 + i, 1000)));
            }
        }

        [Test]
        public override void AcceptsVolumeRenkoBarsAsInput()
        {
            var indicator = CreateIndicator();
            var start = new DateTime(2020, 1, 1);

            for (int i = 0; i < 10; i++)
            {
                var time = start.AddDays(i);
                Assert.DoesNotThrow(() => indicator.Update(
                    new RenkoBar(_spy, time, time, 100 + i, 101 + i, 99 + i, 100 + i, 1000)));
                Assert.DoesNotThrow(() => indicator.Update(
                    new RenkoBar(_qqq, time, time, 100 + i, 101 + i, 99 + i, 100 + i, 1000)));
            }
        }
        [Test]
        public void SimpleCovarianceVerification()
        {
            // Test that Cov(X, X) == Variance(Returns(X))
            // We use _qqq as the reference symbol but we will feed it the SAME data as _spy
            var cov = new Covariance("cov", _spy, _qqq, 4);

            var reference = DateTime.Today;
            
            // 1. Constant Returns
            // Prices: 100, 110, 121, 133.1, 146.41
            // Returns: 0.1, 0.1, 0.1, 0.1
            decimal price = 100m;
            for (int i = 0; i < 5; i++)
            {
                var time = reference.AddDays(i);
                cov.Update(new TradeBar(time, _spy, price, price, price, price, 100));
                cov.Update(new TradeBar(time, _qqq, price, price, price, price, 100));
                price *= 1.1m;
            }

            Assert.IsTrue(cov.IsReady);
            Assert.AreEqual(0.0, (double)cov.Current.Value, 1e-9);

            // 2. Varying Returns
            // Returns: 0.1, 0.2, 0.1, 0.2
            // Variance of {0.1, 0.2, 0.1, 0.2}
            // Mean = 0.15
            // Diff = -0.05, 0.05, -0.05, 0.05
            // SqDiff = 0.0025 * 4 = 0.01
            // Variance = 0.01 / 3 = 0.0033333333
            
            cov.Reset();
            price = 100m;
            // P0
            cov.Update(new TradeBar(reference, _spy, price, price, price, price, 100));
            cov.Update(new TradeBar(reference, _qqq, price, price, price, price, 100));
            
            double[] returns = { 0.1, 0.2, 0.1, 0.2 };
            for(int i=0; i<4; i++)
            {
                price = price * (decimal)(1.0 + returns[i]);
                var time = reference.AddDays(i+1);
                cov.Update(new TradeBar(time, _spy, price, price, price, price, 100));
                cov.Update(new TradeBar(time, _qqq, price, price, price, price, 100));
            }
            
            Assert.AreEqual(0.0033333333d, (double)cov.Current.Value, 1e-5);
        }

        [Test]
        public override void ComparesAgainstExternalData()
        {
            var indicator = CreateIndicator();
            var path = Path.Combine("TestData", TestFileName);

            if (!File.Exists(path))
            {
                path = TestFileName;
            }

            if (!File.Exists(path))
            {
                Assert.Ignore("Skipping test: CSV file not found.");
            }

            var lines = File.ReadAllLines(path);
            var header = lines[0]; // Skip header
            
            int successCount = 0;

            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(',');

                var symbol = parts[0];
                var dateStr = parts[1].Trim();
                DateTime date;
                if (dateStr.Length > 8)
                {
                    date = DateTime.ParseExact(dateStr, "yyyyMMdd HH:mm", CultureInfo.InvariantCulture);
                }
                else
                {
                    date = DateTime.ParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                var open = decimal.Parse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                var high = decimal.Parse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                var low = decimal.Parse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                var close = decimal.Parse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                var volume = decimal.Parse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                var expectedCovariance = decimal.Parse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture);

                var currentSymbol = symbol == "SPY" ? _spy : _qqq;
                indicator.Update(new TradeBar(date, currentSymbol, open, high, low, close, volume));

                // Only verify if the indicator actually updated its current value to this date
                // MultiSymbolIndicator waits for all symbols to be present before updating
                if (indicator.IsReady && indicator.Current.EndTime == date)
                {
                    Assert.AreEqual((double)expectedCovariance, (double)indicator.Current.Value, 1e-4,
                        $"Date: {date:yyyy-MM-dd}, Expected: {expectedCovariance}, Actual: {indicator.Current.Value}");
                }
            }
        }
    }
}