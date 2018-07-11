using System;

namespace TDCD_FDR_Calculator
{
    public partial class Program
    {
        public class ScoreRow : IScoreRow
        {
            public ScoreRow() { }

            public ScoreRow(string line)
            {
                string[] cells = line.Split(',');

                this.Tag = cells[0];
                this.Score = Convert.ToDouble(cells[1]);
            }

            public object Tag { get; set; }
            public double Score { get; set; }
            public double EmpiricalQValue { get; set; }
            public double AnalyticalQValue { get; set; }
            public double EnhancedQValue { get; set; }
        }
    }
}