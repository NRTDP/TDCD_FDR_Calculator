using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Integration;
using System;
using System.IO;
using System.Linq;

namespace TDCD_FDR_Calculator
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: FdrEstimator.exe forwardFilePath decoyFilePath outputFilePath");
                return;
            }

            new Program().Run(args);
        }

        public void Run(string[] args)
        {
            string forwardFilePath = args[0];
            string decoyFilePath = args[1];
            string outputFilePath = args[2];

            ScoreRow[] forwards = File.ReadAllLines(forwardFilePath).Select(x => new ScoreRow(x)).ToArray();
            ScoreRow[] decoys = File.ReadAllLines(decoyFilePath).Select(x => new ScoreRow(x)).ToArray();

            // Calculate both Q-values
            Console.WriteLine("Calculating Empirical Q-Values.");
            this.CalculateEmpiricalQValues(forwards, decoys);

            Console.WriteLine("Calculating Analytical Q-Values.");
            this.CalculateAnalyticalQValues(forwards, decoys);

            Console.WriteLine("Calculating Enhanced Q-Values.");
            this.CalculateEnhancedQValues(forwards, decoys.Max(x => x.Score));

            // Write out results
            File.WriteAllLines(outputFilePath, forwards.Select(x => $"{x.Tag},{x.Score},{x.EmpiricalQValue},{x.EnhancedQValue}"));
        }

        public void CalculateEmpiricalQValues(IScoreRow[] forwardHits, IScoreRow[] decoyScores)
        {
            int numForwards = forwardHits.Length;
            int numDecoys = decoyScores.Length;

            int decoyIndex = numDecoys - 1;
            double previousQValue = 1;

            for (int forwardIndex = numForwards - 1; forwardIndex >= 0; forwardIndex--)
            {
                var forwardHit = forwardHits[forwardIndex];

                // decoyIndex needs to be at the smallest score greater than or equal to forwardScore.
                while (decoyIndex >= 0 && decoyScores[decoyIndex].Score < forwardHit.Score)
                    decoyIndex--;

                int decoyRank = decoyIndex + 1;
                int forwardRank = forwardIndex + 1;

                // Intuitively would use r/n for postProb, but supposedly (r+1)/(n+1) is better.  This also avoids a 0 at the better scores.  https://www.ncbi.nlm.nih.gov/pmc/articles/PMC379178/
                double postProb = (double)(decoyRank + 1) / (numDecoys + 1);
                double fdrAtScore = postProb * numForwards / forwardRank;

                // The qValue is defined as the minimum FDR at which the result would be considered significant.
                double qValue = Math.Min(previousQValue, fdrAtScore);
                previousQValue = qValue;

                forwardHit.EmpiricalQValue = qValue;
            }
        }

        public void CalculateAnalyticalQValues(IScoreRow[] forwardHits, IScoreRow[] decoyScores)
        {
            Gamma gamma = this.FitGammaDistributionMaximumLikelihood(decoyScores);

            int rank = 1;
            int count = forwardHits.Length;

            foreach (IScoreRow forwardScore in forwardHits)
            {
                if (forwardScore.Score >= 0)
                {
                    double score = forwardScore.Score;
                    double postProb = this.CalcPostProb(score, gamma);

                    forwardScore.AnalyticalQValue = postProb * count / rank;
                    rank++;
                }
            }
        }
        private Gamma FitGammaDistributionMaximumLikelihood(IScoreRow[] data)
        {
            // First iterate through the decoy scores and calculate some necessary values from them.
            double sum = 0;
            double lnSum = 0;
            long count = 0;

            foreach (IScoreRow row in data)
            // For better accuracy, these could be sorted, but that would make this slower.  It shouldn't be necessary since these values are probably not small.
            {
                if (row.Score > 0)
                {
                    sum += row.Score;
                    lnSum += Math.Log(row.Score);
                    count++;
                }
            }

            return this.FitGammaDistributionMaximumLikelihood(sum, lnSum, count);
        }
        private Gamma FitGammaDistributionMaximumLikelihood(double sum, double naturalLogSum, long count, double shapeError = 1E-10)
        {
            if (count <= 0)
                throw new ArgumentException("count must be greater than 0", "count");

            double average = sum / count;
            double averageLn = naturalLogSum / count;
            double s = Math.Log(average) - averageLn;

            if (s <= 0)
            {
                throw new ArgumentException("The natural log of sum must be greater than naturalLogSum.");
            }

            // First approximation of the shape.
            double shape = (3 - s + Math.Sqrt((s - 3) * (s - 3) + 24 * s)) / (12 * s);

            // Refine approximation using Newton's Method.
            bool accurateEnough = false;
            while (!accurateEnough)
            {
                double newShape = NewtonApproxGammaShape(shape, s);

                //if (Math.Abs(newShape - shape) <= _shapeError)
                if (this.EqualWithinRelativeEpsilon(shape, newShape, shapeError))
                {
                    accurateEnough = true;
                }

                shape = newShape;
            }

            // Now calculate rate.
            double rate = shape / average;

            return new Gamma(shape, rate);
        }
        private bool EqualWithinRelativeEpsilon(double x, double y, double relativeEpsilon = 1E-15)
        {
            if (x == y)
                return true;

            double absX = Math.Abs(x);
            double absY = Math.Abs(y);

            if (absX > absY)
                return Math.Abs(x - y) < absX * relativeEpsilon;

            return Math.Abs(x - y) < absY * relativeEpsilon;
        }
        private double NewtonApproxGammaShape(double shape, double s)
        {
            double digamma = SpecialFunctions.DiGamma(shape);
            return shape - (Math.Log(shape) - digamma - s) / (1 / shape - this.Trigamma(shape));
        }
        private double Trigamma(double z)
        {
            Func<double, double> func = x => Math.Pow(x, z - 1) * Math.Log(x) / (1 - x);

            return -DoubleExponentialTransformation.Integrate(func, 0, 1, 1E-5);
        }

        private double CalcPostProb(double score, Gamma distribution)
        {
            return this.GetUpperGamma(score, distribution) / this.GetTotalGamma(distribution);
        }
        private double GetUpperGamma(double score, Gamma distribution)
        {
            return SpecialFunctions.GammaUpperIncomplete(distribution.Shape,
                distribution.Rate * score);
        }
        private double GetTotalGamma(Gamma distribution)
        {
            return SpecialFunctions.Gamma(distribution.Shape);
        }

        private void CalculateEnhancedQValues(IScoreRow[] forwardHits, double bestDecoyScore)
        {
            for (int i = 0; i < forwardHits.Length; i++)
            {
                // If the forward is better than ALL decoys, then use Gamma approx., else use empiric values
                if (forwardHits[i].Score >= bestDecoyScore)
                    forwardHits[i].EnhancedQValue = forwardHits[i].AnalyticalQValue;
                else
                    forwardHits[i].EnhancedQValue = forwardHits[i].EmpiricalQValue;
            }
        }
    }
}