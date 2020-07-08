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
        private int consecutiveWinTradeCounter  = 0;
        private Trade lastTrade;
        private Indicators.JiraiyaIndicators.DowTheoryIndicator DowTheoryIndicator1;
        private Dictionary<HourList, TimeSpan> hourDictionary;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description                                     = @"Enter the description for your new custom Strategy here.";
                Name                                            = "Test";
                Calculate                                       = Calculate.OnPriceChange;
                EntriesPerDirection                             = 1;
                EntryHandling                                   = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy                    = true;
                ExitOnSessionCloseSeconds                       = 30;
                IsFillLimitOnTouch                              = false;
                MaximumBarsLookBack                             = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution                             = OrderFillResolution.Standard;
                Slippage                                        = 0;
                StartBehavior                                   = StartBehavior.WaitUntilFlat;
                TimeInForce                                     = TimeInForce.Gtc;
                TraceOrders                                     = false;
                RealtimeErrorHandling                           = RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
                StopTargetHandling                              = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade                             = 0;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration       = IsInstantiatedOnEachOptimizationIterationIsh;

                CalculationTypeDT                               = CalculationTypeListDowTheory.Pivot;
                CalculationTypePCW                              = CalculationTypeListPriceActionSwing.SwingForward;
                Strength                                        = 2;
                MaxPercentOfPivotRetraction                     = 100;
                MinPercentOfPivotRetraction                     = 0;
                MinTime                                         = HourList.hr01h00;
                MaxTime                                         = HourList.hr12h00;
                PlotOnChart                                     = true;
                IsInstantiatedOnEachOptimizationIterationIsh    = true;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                DowTheoryIndicator1                 = DowTheoryIndicator(Close, CalculationTypeDT, CalculationTypePCW, Strength, true,
                                                                         MaxPercentOfPivotRetraction, MinPercentOfPivotRetraction);
                hourDictionary                      = new Dictionary<HourList, TimeSpan>();
                
                if(PlotOnChart)
                    AddChartIndicator(DowTheoryIndicator1);
                CreateDictionary();
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0)
                return;

            if (CurrentBars[0] < 1)
                return;

            // Prevents opening another operation while one is already taking place
            if (Position.MarketPosition != MarketPosition.Flat)
                return;

            // Disable trading outside the time range
            if (!(Times[0][0].TimeOfDay > hourDictionary[MinTime] &&
                 Times[0][0].TimeOfDay < hourDictionary[MaxTime]))
                return;

            // Set 1
            if (DowTheoryIndicator1.LongShortSignalIsh == OrderSideSignal.Buy)
            {
                string longOrderID = SideTrade.Long + " " + CurrentBar;
                EnterLong(Convert.ToInt32(DefaultQuantity), longOrderID);
                SetStopLossAndProfitTarget(SideTrade.Long, longOrderID);

                //This line prevents the same signal open another order in the same bar
                DowTheoryIndicator1.ResetLongShortSignal();
            }

            // Set 2
            if (DowTheoryIndicator1.LongShortSignalIsh == OrderSideSignal.Sell)
            {
                string shortOrderID = SideTrade.Short + " " + CurrentBar;
                EnterShort(Convert.ToInt32(DefaultQuantity), shortOrderID);
                SetStopLossAndProfitTarget(SideTrade.Short, shortOrderID);

                //This line prevents the same signal open another order in the same bar
                DowTheoryIndicator1.ResetLongShortSignal();
            }

            // Test and increment the consecutive counter
            if (SystemPerformance.AllTrades.Count != 0 && SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitTicks > 0 &&
                SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1] != lastTrade)
            {
                consecutiveWinTradeCounter++;
                lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
            }
            else if(SystemPerformance.AllTrades.Count != 0 && SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitTicks < 0)
            {
                consecutiveWinTradeCounter = 0;
            }

            // Criar c�digo para calcular quantidade de contratos utilizando estrat�gia de soros

            // Calcular pre�o do tick ou pip
            // Multiplicar pela quantidade de lotes

            // Criar c�digo para aplicar estrat�gia de soros

            //PrintStrategyStatus();
        }

        /// <summary>
        /// https://ninjatrader.com/support/helpGuides/nt8/?realtimeerrorhandling.htm
        /// </summary>
        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, 
                                              int quantity, int filled, double averageFillPrice, 
                                              OrderState orderState, DateTime time, ErrorCode error, string comment)
        {
            if (order.OrderState == OrderState.Rejected)
            {
                Code.Output.Process(time + "    " + error, PrintTo.OutputTab2);
                
                switch(Position.MarketPosition)
                {
                    case MarketPosition.Long:
                        ExitLong("Panic order", "");
                        break;

                    case MarketPosition.Short:
                        ExitShort("Panic order", "");
                        break;
                }
            }
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

        private void PrintStrategyStatus()
        {
            if (SystemPerformance.AllTrades.Count != 0)
                Print(Times[0][0].Date.ToString("dd/MM/yyyy") + "    " +
                      Times[0][0].TimeOfDay + "    " +
                      "Current time: " + DateTime.Now.ToString("HH:mm:ss") + "    " +
                      SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitCurrency + "    " +
                      "Current consecutive win: " + consecutiveWinTradeCounter);
        }

        private void SetStopLossAndProfitTarget(SideTrade sideTrade, string orderID)
        {
            //----Bearish----|---Bullish---
            //----3----------|----------0--
            //-----\---1-----|-----2---/---
            //------\-/-\----|----/-\-/----
            //-------2---\---|---/---1-----
            //------------0--|--3----------

            MatrixPoints lastMastrix    = DowTheoryIndicator1.LastMatrix;
            Point pointZero             = lastMastrix.PointsList[0];
            Point pointOne              = lastMastrix.PointsList[1];
            Point pointTwo              = lastMastrix.PointsList[2];

            switch (sideTrade)
            {
                case SideTrade.Long:
                    Draw.Line(this, "Stop loss line " + pointOne.Index,
                              ConvertBarIndexToBarsAgo(this, pointTwo.BarIndex), pointOne.Price,
                              ConvertBarIndexToBarsAgo(this, pointZero.BarIndex), pointOne.Price, Brushes.Green);

                    // Definir um ponto para o stop
                    SetStopLoss(orderID, CalculationMode.Price, pointOne.Price, false);

                    double longTargetPrice = pointOne.Price - pointTwo.Price < 0 ?
                                             (pointOne.Price - pointTwo.Price) * -1 : pointOne.Price - pointTwo.Price;

                    longTargetPrice += pointTwo.Price;

                    Draw.Line(this, "Profit target line " + pointOne.Index,
                              ConvertBarIndexToBarsAgo(this, pointTwo.BarIndex), longTargetPrice,
                              ConvertBarIndexToBarsAgo(this, pointZero.BarIndex), longTargetPrice, Brushes.Green);

                    SetProfitTarget(orderID, CalculationMode.Price, longTargetPrice, false);
                    break;

                case SideTrade.Short:
                    Draw.Line(this, "Stop loss line " + pointOne.Index,
                              ConvertBarIndexToBarsAgo(this, pointTwo.BarIndex), pointOne.Price,
                              ConvertBarIndexToBarsAgo(this, pointZero.BarIndex), pointOne.Price, Brushes.Red);

                    // Definir um ponto para o stop
                    SetStopLoss(orderID, CalculationMode.Price, pointOne.Price, false);

                    double shortTargetPrice = pointOne.Price - pointTwo.Price < 0 ?
                                             (pointOne.Price - pointTwo.Price) * -1 : pointOne.Price - pointTwo.Price;

                    shortTargetPrice -= pointTwo.Price;
                    shortTargetPrice *= -1;

                    Draw.Line(this, "Profit target line " + pointOne.Index,
                              ConvertBarIndexToBarsAgo(this, pointTwo.BarIndex), shortTargetPrice,
                              ConvertBarIndexToBarsAgo(this, pointZero.BarIndex), shortTargetPrice, Brushes.Red);

                    SetProfitTarget(orderID, CalculationMode.Price, shortTargetPrice);
                    break;
            }
        }

        private static int ConvertBarIndexToBarsAgo(NinjaScriptBase owner, int barIndex)
        {
            return (barIndex - owner.CurrentBar) < 0 ? (barIndex - owner.CurrentBar) * -1 : barIndex - owner.CurrentBar;
        }

        private void CreateDictionary()
        {
            hourDictionary.Add(HourList.hr00h00, new TimeSpan(00, 00, 00));
            hourDictionary.Add(HourList.hr00h30, new TimeSpan(00, 30, 00));
            hourDictionary.Add(HourList.hr01h00, new TimeSpan(01, 00, 00));
            hourDictionary.Add(HourList.hr01h30, new TimeSpan(01, 30, 00));
            hourDictionary.Add(HourList.hr02h00, new TimeSpan(02, 00, 00));
            hourDictionary.Add(HourList.hr02h30, new TimeSpan(02, 30, 00));
            hourDictionary.Add(HourList.hr03h00, new TimeSpan(03, 00, 00));
            hourDictionary.Add(HourList.hr03h30, new TimeSpan(03, 30, 00));
            hourDictionary.Add(HourList.hr04h00, new TimeSpan(04, 00, 00));
            hourDictionary.Add(HourList.hr04h30, new TimeSpan(04, 30, 00));
            hourDictionary.Add(HourList.hr05h00, new TimeSpan(05, 00, 00));
            hourDictionary.Add(HourList.hr05h30, new TimeSpan(05, 30, 00));
            hourDictionary.Add(HourList.hr06h00, new TimeSpan(06, 00, 00));
            hourDictionary.Add(HourList.hr06h30, new TimeSpan(06, 30, 00));
            hourDictionary.Add(HourList.hr07h00, new TimeSpan(07, 00, 00));
            hourDictionary.Add(HourList.hr07h30, new TimeSpan(07, 30, 00));
            hourDictionary.Add(HourList.hr08h00, new TimeSpan(08, 00, 00));
            hourDictionary.Add(HourList.hr08h30, new TimeSpan(08, 30, 00));
            hourDictionary.Add(HourList.hr09h00, new TimeSpan(09, 00, 00));
            hourDictionary.Add(HourList.hr09h30, new TimeSpan(09, 30, 00));
            hourDictionary.Add(HourList.hr10h00, new TimeSpan(10, 00, 00));
            hourDictionary.Add(HourList.hr10h30, new TimeSpan(10, 30, 00));
            hourDictionary.Add(HourList.hr11h00, new TimeSpan(11, 00, 00));
            hourDictionary.Add(HourList.hr11h30, new TimeSpan(11, 30, 00));
            hourDictionary.Add(HourList.hr12h00, new TimeSpan(12, 00, 00));
            hourDictionary.Add(HourList.hr12h30, new TimeSpan(12, 30, 00));
            hourDictionary.Add(HourList.hr13h00, new TimeSpan(13, 00, 00));
            hourDictionary.Add(HourList.hr13h30, new TimeSpan(13, 30, 00));
            hourDictionary.Add(HourList.hr14h00, new TimeSpan(14, 00, 00));
            hourDictionary.Add(HourList.hr14h30, new TimeSpan(14, 30, 00));
            hourDictionary.Add(HourList.hr15h00, new TimeSpan(15, 00, 00));
            hourDictionary.Add(HourList.hr15h30, new TimeSpan(15, 30, 00));
            hourDictionary.Add(HourList.hr16h00, new TimeSpan(16, 00, 00));
            hourDictionary.Add(HourList.hr16h30, new TimeSpan(16, 30, 00));
            hourDictionary.Add(HourList.hr17h00, new TimeSpan(17, 00, 00));
            hourDictionary.Add(HourList.hr17h30, new TimeSpan(17, 30, 00));
            hourDictionary.Add(HourList.hr18h00, new TimeSpan(18, 00, 00));
            hourDictionary.Add(HourList.hr18h30, new TimeSpan(18, 30, 00));
            hourDictionary.Add(HourList.hr19h00, new TimeSpan(19, 00, 00));
            hourDictionary.Add(HourList.hr19h30, new TimeSpan(19, 30, 00));
            hourDictionary.Add(HourList.hr20h00, new TimeSpan(20, 00, 00));
            hourDictionary.Add(HourList.hr20h30, new TimeSpan(20, 30, 00));
            hourDictionary.Add(HourList.hr21h00, new TimeSpan(21, 00, 00));
            hourDictionary.Add(HourList.hr21h30, new TimeSpan(21, 30, 00));
            hourDictionary.Add(HourList.hr22h00, new TimeSpan(22, 00, 00));
            hourDictionary.Add(HourList.hr22h30, new TimeSpan(22, 30, 00));
            hourDictionary.Add(HourList.hr23h00, new TimeSpan(23, 00, 00));
            hourDictionary.Add(HourList.hr23h30, new TimeSpan(23, 30, 00));
            hourDictionary.Add(HourList.hr23h59, new TimeSpan(23, 59, 00));
        }

        public enum SideTrade
        {
            Long,
            Short
        }

        public enum HourList
        {
            hr00h00,
            hr00h30,
            hr01h00,
            hr01h30,
            hr02h00,
            hr02h30,
            hr03h00,
            hr03h30,
            hr04h00,
            hr04h30,
            hr05h00,
            hr05h30,
            hr06h00,
            hr06h30,
            hr07h00,
            hr07h30,
            hr08h00,
            hr08h30,
            hr09h00,
            hr09h30,
            hr10h00,
            hr10h30,
            hr11h00,
            hr11h30,
            hr12h00,
            hr12h30,
            hr13h00,
            hr13h30,
            hr14h00,
            hr14h30,
            hr15h00,
            hr15h30,
            hr16h00,
            hr16h30,
            hr17h00,
            hr17h30,
            hr18h00,
            hr18h30,
            hr19h00,
            hr19h30,
            hr20h00,
            hr20h30,
            hr21h00,
            hr21h30,
            hr22h00,
            hr22h30,
            hr23h00,
            hr23h30,
            hr23h59
        }

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "Dow theory calculation type", Order = 0, GroupName = "Parameters")]
        public CalculationTypeListDowTheory CalculationTypeDT
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Price action swing calculation type", Order = 1, GroupName = "Parameters")]
        public CalculationTypeListPriceActionSwing CalculationTypePCW
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
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

        [NinjaScriptProperty]
        [Display(Name = "Min time", Order = 5, GroupName = "Parameters")]
        public HourList MinTime
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Max time", Order = 6, GroupName = "Parameters")]
        public HourList MaxTime
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Plot on chart", Order = 7, GroupName = "Parameters")]
        public bool PlotOnChart
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Is Instantiated On Each Optimization Iteration", Order = 8, GroupName = "Parameters")]
        public bool IsInstantiatedOnEachOptimizationIterationIsh
        { get; set; }

        #endregion
    }
}
