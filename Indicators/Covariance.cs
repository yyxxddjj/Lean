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
using QuantConnect.Data.Market;
using MathNet.Numerics.Statistics;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Covariance indicator that measures the directional relationship between two symbols' returns.
    /// The indicator uses a rolling window of the specified period to calculate the covariance.
    /// </summary>
    public class Covariance : DualSymbolIndicator<IBaseDataBar>
    {
        /// <summary>
        /// RollingWindow of returns of the target symbol in the given period
        /// </summary>
        private readonly RollingWindow<double> _targetReturns;

        /// <summary>
        /// RollingWindow of returns of the reference symbol in the given period
        /// </summary>
        private readonly RollingWindow<double> _referenceReturns;

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _targetReturns.IsReady && _referenceReturns.IsReady;

        /// <summary>
        /// Creates a new Covariance indicator with the specified name, target, reference and period
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public Covariance(string name, Symbol targetSymbol, Symbol referenceSymbol, int period)
            : base(name, targetSymbol, referenceSymbol, 2)
        {
            if (period < 2)
            {
                throw new ArgumentException($"Period must be greater than 1 but was {period}", nameof(period));
            }
            
            _targetReturns = new RollingWindow<double>(period);
            _referenceReturns = new RollingWindow<double>(period);
            // We need 1 extra bar to calculate the first return
            WarmUpPeriod = period + 1;
        }

        /// <summary>
        /// Creates a new Covariance indicator with the default name, target, reference and period
        /// </summary>
        /// <param name="targetSymbol">The target symbol of this indicator</param>
        /// <param name="referenceSymbol">The reference symbol of this indicator</param>
        /// <param name="period">The period of this indicator</param>
        public Covariance(Symbol targetSymbol, Symbol referenceSymbol, int period)
            : this($"COV({period})", targetSymbol, referenceSymbol, period)
        {
        }

        /// <summary>
        /// Computes the returns with the new given data point and the last given data point
        /// </summary>
        /// <param name="rollingWindow">The collection of data points from which we want
        /// to compute the return</param>
        /// <returns>The returns with the new given data point</returns>
        private static double GetNewReturn(IReadOnlyWindow<IBaseDataBar> rollingWindow)
        {
            return (double)(rollingWindow[0].Close.SafeDivision(rollingWindow[1].Close) - 1);
        }

        /// <summary>
        /// Computes the covariance between the target and reference symbol returns
        /// </summary>
        protected override decimal ComputeIndicator()
        {
            if (TargetDataPoints.Count >= 2)
            {
                _targetReturns.Add(GetNewReturn(TargetDataPoints));
            }

            if (ReferenceDataPoints.Count >= 2)
            {
                _referenceReturns.Add(GetNewReturn(ReferenceDataPoints));
            }

            if (!IsReady)
            {
                return 0m;
            }

            // Calculate covariance of returns using MathNet.Numerics extension
            var covariance = _targetReturns.Covariance(_referenceReturns);

            // Handle edge cases
            if (double.IsNaN(covariance) || double.IsInfinity(covariance))
            {
                return 0m;
            }

            return Extensions.SafeDecimalCast(covariance);
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _targetReturns.Reset();
            _referenceReturns.Reset();
            base.Reset();
        }
    }
}