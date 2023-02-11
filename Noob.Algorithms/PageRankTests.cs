// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2023-02-11
//
// Last Modified By : noob
// Last Modified On : 2023-02-11
// ***********************************************************************
// <copyright file="PageRankTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
/// <summary>
/// The Algorithms namespace.
/// </summary>

using MathNet.Numerics.LinearAlgebra;
using NUnit.Framework;

namespace Noob.Algorithms
{
    /// <summary>
    /// Class PageRankTests.
    /// </summary>
    [TestFixture]
    public class PageRankTests
    {
        /// <summary>
        /// Defines the test method ComputePageRank.
        /// </summary>
        [TestCase]
        public void ComputePageRank()
        {
            double[,] matrix = {
                { 0, 1.0 / 3, 1.0 / 3, 1.0 / 3 },
                { 1.0 / 3, 0, 1.0 / 3, 1.0 / 3 },
                { 1.0 / 3, 1.0 / 3, 0, 1.0 / 3 },
                { 1.0 / 3, 1.0 / 3, 1.0 / 3, 0 } 
            };

            double dampingFactor = 0.85;
            int maxIterations = 100;

            PageRank pageRank = new PageRank(matrix, dampingFactor, maxIterations);
            double[] result = pageRank.ComputePageRank();

            Console.WriteLine("Page Rank Results:");
            for (int i = 0; i < result.Length; i++)
            {
                Console.WriteLine($"Page {(i + 1)} : {result[i]}");
            }
        }

        /// <summary>
        /// Defines the test method ComputePageRank.
        /// </summary>
        [TestCase]
        public void ComputePageRank2()
        {
            var transitionMatrix = Matrix<double>.Build.DenseOfArray(new double[,] {
                { 0, 1 / 2.0, 1 / 2.0, 0 },
                { 1 / 3.0, 0, 0, 1 / 2.0 },
                { 1 / 3.0, 0, 0, 1 / 2.0 },
                { 1 / 3.0, 1 / 2.0, 1 / 2.0, 0 }
            });

            var epsilon = 0.0001;
            var dampingFactor = 0.85;
            var teleportingProbability = (1 - dampingFactor) / transitionMatrix.RowCount;

            var personalizationVector = Vector<double>.Build.Dense(transitionMatrix.RowCount, 1.0 / transitionMatrix.RowCount);

            var powerMethod = new PowerMethod(transitionMatrix,dampingFactor, teleportingProbability, personalizationVector, epsilon);
            var result = powerMethod.ComputePageRank();

            Console.WriteLine("PageRank Result:");
            for (int i = 0; i < result.Count; i++)
            {
                Console.WriteLine("Page " + (i + 1) + ": " + result[i]);
            }
        }
    }

    /// <summary>
    /// Class PageRank.
    /// </summary>
    public class PageRank
    {
        /// <summary>
        /// The n
        /// </summary>
        private readonly int N;
        /// <summary>
        /// The matrix
        /// </summary>
        private readonly double[,] matrix;
        /// <summary>
        /// The page rank
        /// </summary>
        private double[] pageRank;
        /// <summary>
        /// The damping factor
        /// </summary>
        private readonly double dampingFactor;
        /// <summary>
        /// The maximum iterations
        /// </summary>
        private readonly int maxIterations;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageRank" /> class.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="dampingFactor">The damping factor.</param>
        /// <param name="maxIterations">The maximum iterations.</param>
        public PageRank(double[,] matrix, double dampingFactor, int maxIterations)
        {
            N = matrix.GetLength(0);
            this.matrix = matrix;
            pageRank = new double[N];
            this.dampingFactor = dampingFactor;
            this.maxIterations = maxIterations;
        }

        /// <summary>
        /// Computes the page rank.
        /// </summary>
        /// <returns>System.Double[].</returns>
        public double[] ComputePageRank()
        {
            for (int i = 0; i < N; i++)
            {
                pageRank[i] = 1.0 / N;
            }

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                double[] nextPageRank = new double[N];
                for (int i = 0; i < N; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < N; j++)
                    {
                        sum += matrix[j, i] * pageRank[j];
                    }
                    nextPageRank[i] = (1 - dampingFactor) / N + dampingFactor * sum;
                }
                pageRank = nextPageRank;
            }

            return pageRank;
        }
    }

    /// <summary>
    /// Class PowerMethod.
    /// </summary>
    public class PowerMethod
    {
        /// <summary>
        /// The transition matrix
        /// </summary>
        private readonly Matrix<double> _transitionMatrix;
        /// <summary>
        /// The teleporting probability
        /// </summary>
        private readonly double _teleportingProbability;
        /// <summary>
        /// The personalization vector
        /// </summary>
        private readonly Vector<double> _personalizationVector;
        /// <summary>
        /// The epsilon
        /// </summary>
        private readonly double _epsilon;
        /// <summary>
        /// The damping factor
        /// </summary>
        private readonly double _dampingFactor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerMethod"/> class.
        /// </summary>
        /// <param name="transitionMatrix">The transition matrix.</param>
        /// <param name="dampingFactor">The damping factor.</param>
        /// <param name="teleportingProbability">The teleporting probability.</param>
        /// <param name="personalizationVector">The personalization vector.</param>
        /// <param name="epsilon">The epsilon.</param>
        public PowerMethod(Matrix<double> transitionMatrix, double dampingFactor, double teleportingProbability, Vector<double> personalizationVector, double epsilon)
        {
            _transitionMatrix = transitionMatrix;
            _dampingFactor= dampingFactor;
            _teleportingProbability = teleportingProbability;
            _personalizationVector = personalizationVector;
            _epsilon = epsilon;
            
        }

        /// <summary>
        /// Computes the page rank.
        /// </summary>
        /// <returns>Vector&lt;System.Double&gt;.</returns>
        public Vector<double> ComputePageRank()
        {
            var currentPageRank = Vector<double>.Build.Dense(_transitionMatrix.RowCount, 1.0 / _transitionMatrix.RowCount);
            var nextPageRank = Vector<double>.Build.Dense(_transitionMatrix.RowCount, 0);

            while (true)
            {
                nextPageRank = _teleportingProbability + _dampingFactor * (_transitionMatrix.Transpose() * currentPageRank);
                var difference = (nextPageRank - currentPageRank).L2Norm();

                if (difference < _epsilon)
                {
                    break;
                }

                currentPageRank = nextPageRank;
            }

            return nextPageRank;
        }
    }
}