namespace TDCD_FDR_Calculator
{
    public interface IScoreRow
    {
        double AnalyticalQValue { get; set; }
        double EmpiricalQValue { get; set; }
        double EnhancedQValue { get; set; }
        double Score { get; }
        object Tag { get; }
    }
}