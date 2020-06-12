#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Custom.Indicators.JiraiyaIndicators;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.JiraiyaStrategies
{
	public class Test : Strategy
	{
        private const int Buy = 1;
        private const int Sell = -1;

        private int consecutiveWinTradeCounter = 0;
        private Trade lastTrade;

        private NinjaTrader.NinjaScript.Indicators.JiraiyaIndicators.DowTheoryIndicator DowTheoryIndicator1;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Strategy here.";
                Name = "Test";
                Calculate = Calculate.OnPriceChange;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;

                CalculationTypeDT = CalculationTypeListDowTheory.Pivot;
                CalculationTypePCW = CalculationTypeList.SwingForward;
                Strength = 2;
                MaxPercentOfPivotRetraction = 80;
                MinPercentOfPivotRetraction = 20;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                DowTheoryIndicator1 = DowTheoryIndicator(Close, CalculationTypeDT, CalculationTypePCW, Strength, true,
                                                         MaxPercentOfPivotRetraction, MinPercentOfPivotRetraction);
                DowTheoryIndicator1.Plots[0].Brush = Brushes.Transparent;
                AddChartIndicator(DowTheoryIndicator1);
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < 1)
                return;

            // Disable trading outside the time range
            if (!(Times[0][0].TimeOfDay > new TimeSpan(01, 00, 00) &&
                 Times[0][0].TimeOfDay < new TimeSpan(11, 59, 00)))
                return;

            // Set 1
            if (DowTheoryIndicator1[0] == Buy)
            {
                EnterLong(Convert.ToInt32(DefaultQuantity), "");
                SetStopLossAndProfitTarget(SideTrade.Long);
            }

            // Set 2
            if (DowTheoryIndicator1[0] == Sell)
            {
                EnterShort(Convert.ToInt32(DefaultQuantity), "");
                SetStopLossAndProfitTarget(SideTrade.Short);
            }

            // Test and increment the consecutive counter
            if(SystemPerformance.AllTrades.Count != 0 && SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitTicks > 0 &&
                SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1] != lastTrade)
            {
                consecutiveWinTradeCounter++;
                lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
            }
            else if(SystemPerformance.AllTrades.Count != 0 && SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitTicks < 0)
            {
                consecutiveWinTradeCounter = 0;
            }

            // Criar código para calcular quantidade de contratos utilizando estratégia de soros

            // Calcular preço do tick ou pip
            // Multiplicar pela quantidade de lotes

            // Criar código para aplicar estratégia de soros

            if (SystemPerformance.AllTrades.Count != 0)
                Print(Times[0][0].Date + "    " + Times[0][0].TimeOfDay + "    " + CurrentBar + "   " +
                      SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitTicks.ToString("N5") + "    " +
                      consecutiveWinTradeCounter + "     " +
                      (TickValueForUSDBase * 10).ToString("N5") + 
                      "    TickSize: " + TickSize.ToString("N5"));
        }

        private double TickValueForUSDQuote
        {
            get
            {
                return TickSize;
            }
        }

        private double TickValueForUSDBase
        {
            get
            {
                return TickSize / Close[0];
            }
        }

        private void SetStopLossAndProfitTarget(SideTrade sideTrade)
        {
            //----Bearish----|---Bullish---
            //----3----------|----------0--
            //-----\---1-----|-----2---/---
            //------\-/-\----|----/-\-/----
            //-------2---\---|---/---1-----
            //------------0--|--3----------

            MatrixPoints lastMastrix = DowTheoryIndicator1.LastMatrix;

            // Definir dois pontos que vou utilizar como referencia para a projeção do alvo
            Point pointOne = lastMastrix.PointsList[1];
            Point pointTwo = lastMastrix.PointsList[2];

            // Definir um ponto para o stop
            SetStopLoss("", CalculationMode.Price, pointOne.Price, false);

            switch (sideTrade)
            {
                case SideTrade.Long:
                    double longTargetPrice = pointOne.Price - pointTwo.Price < 0 ?
                                             (pointOne.Price - pointTwo.Price) * -1 : pointOne.Price - pointTwo.Price;

                    longTargetPrice += pointTwo.Price;

                    SetProfitTarget(CalculationMode.Price, longTargetPrice);

                    Draw.Line(this, "Line " + pointOne.Index,
                              ConvertBarIndexToBarsAgo(this, pointTwo.BarIndex), longTargetPrice,
                              ConvertBarIndexToBarsAgo(this, pointOne.BarIndex), longTargetPrice, Brushes.Green);
                    break;
                case SideTrade.Short:
                    double shortTargetPrice = pointOne.Price - pointTwo.Price < 0 ?
                                             (pointOne.Price - pointTwo.Price) * -1 : pointOne.Price - pointTwo.Price;

                    shortTargetPrice -= pointTwo.Price;
                    shortTargetPrice *= -1;

                    SetProfitTarget(CalculationMode.Price, shortTargetPrice);

                    Draw.Line(this, "Line " + pointOne.Index,
                              ConvertBarIndexToBarsAgo(this, pointTwo.BarIndex), shortTargetPrice,
                              ConvertBarIndexToBarsAgo(this, pointOne.BarIndex), shortTargetPrice, Brushes.Red);
                    break;
            }

            // Com os dois pontos calcular o alvo
        }

        private static int ConvertBarIndexToBarsAgo(NinjaScriptBase owner, int barIndex)
        {
            return (barIndex - owner.CurrentBar) < 0 ? (barIndex - owner.CurrentBar) * -1 : barIndex - owner.CurrentBar;
        }

        #region Properties

        [NinjaScriptProperty]
        [Display(Name = "Dow theory calculation type", Order = 0, GroupName = "Parameters")]
        public CalculationTypeListDowTheory CalculationTypeDT
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Price action swing calculation type", Order = 1, GroupName = "Parameters")]
        public CalculationTypeList CalculationTypePCW
        { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Strength", Order = 2, GroupName = "Parameters")]
        public int Strength
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Max percent of pivot retraction", Order = 3, GroupName = "Parameters")]
        public double MaxPercentOfPivotRetraction
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Min percent of pivot retraction", Order = 4, GroupName = "Parameters")]
        public double MinPercentOfPivotRetraction
        { get; set; }
        #endregion
    }

    public enum SideTrade
    {
        Long,
        Short
    }
}
