// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-06-25
//
// Last Modified By : noob
// Last Modified On : 2025-06-25
// ***********************************************************************
// <copyright file="GpsPositionSolverTests.cs" company="Noob.Algorithms">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms
{

    /// <summary>
    /// Class Satellite.
    /// </summary>
    public class Satellite
    {
        /// <summary>
        /// 卫星地心直角坐标（米）
        /// </summary>
        public double X, Y, Z;    // 卫星地心直角坐标（米）
        /// <summary>
        ///  卫星钟差（秒）
        /// </summary>
        public double ClockBias;  // 卫星钟差（秒）
        /// <summary>
        /// 接收机测得该卫星的伪距（米）
        /// </summary>
        public double PseudoRange; // 接收机测得该卫星的伪距（米）
        /// <summary>
        /// 卫星号
        /// </summary>
        public int PRN; // 卫星号
    }

    /// <summary>
    /// Class GpsPositionSolver.
    /// </summary>
    public class GpsPositionSolver
    {
        /// <summary>
        /// The c
        /// </summary>
        public const double C = 299792458.0; // 光速，米/秒

        /// <summary>
        /// 利用四颗及以上卫星伪距进行三维定位
        /// </summary>
        /// <param name="sats">卫星观测列表（需包含伪距+坐标+钟差）</param>
        /// <param name="initPos">初始猜测位置（如[0,0,0]或已知值）</param>
        /// <returns>(X,Y,Z,dt)：地心坐标和接收机钟差</returns>
        public static (double X, double Y, double Z, double Dt) SolvePosition(
            List<Satellite> sats, double[] initPos = null, int maxIter = 10, double tol = 1e-4)
        {
            if (sats == null || sats.Count < 4)
                throw new ArgumentException("至少需要4颗卫星观测。");

            // 1. 初始化变量
            double X = initPos?[0] ?? 0, Y = initPos?[1] ?? 0, Z = initPos?[2] ?? 0, Dt = 0;

            for (int iter = 0; iter < maxIter; iter++)
            {
                int n = sats.Count;
                double[,] A = new double[n, 4];
                double[] L = new double[n];

                for (int i = 0; i < n; i++)
                {
                    var sat = sats[i];
                    double dx = X - sat.X, dy = Y - sat.Y, dz = Z - sat.Z;
                    double R = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    // 理论伪距
                    double prTheo = R + C * (Dt - sat.ClockBias);

                    // 观测-理论
                    L[i] = sat.PseudoRange - prTheo;

                    // 雅可比矩阵（偏导）
                    A[i, 0] = -dx / R;
                    A[i, 1] = -dy / R;
                    A[i, 2] = -dz / R;
                    A[i, 3] = -C;
                }

                // 2. 最小二乘解算：Δx = (A^T A)^{-1} A^T L
                var dX = SolveLeastSquares(A, L);

                // 3. 更新估值
                X += dX[0];
                Y += dX[1];
                Z += dX[2];
                Dt += dX[3];

                // 4. 收敛判断
                if (dX.Take(3).Select(Math.Abs).Max() < tol && Math.Abs(dX[3]) < tol / C)
                    break;
            }
            return (X, Y, Z, Dt);
        }

        /// <summary>
        /// 矩阵最小二乘求解（伪逆），仅作示例，实际建议用成熟线性代数库   
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="L">The l.</param>
        /// <returns>System.Double[].</returns>
        private static double[] SolveLeastSquares(double[,] A, double[] L)
        {
            var n = A.GetLength(0); var m = A.GetLength(1);
            // 构造AT * A与AT * L
            var ATA = new double[m, m];
            var ATL = new double[m];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                    for (int k = 0; k < n; k++)
                        ATA[i, j] += A[k, i] * A[k, j];

            for (int i = 0; i < m; i++)
                for (int k = 0; k < n; k++)
                    ATL[i] += A[k, i] * L[k];

            // 线性方程组求解（高斯消元/矩阵求逆），此处简单处理
            return GaussSolve(ATA, ATL);
        }

        /// <summary>
        /// 高斯消元法，适合小型矩阵，工程建议用专业库
        /// </summary>
        /// <param name="M">The m.</param>
        /// <param name="V">The v.</param>
        /// <returns>System.Double[].</returns>
        private static double[] GaussSolve(double[,] M, double[] V)
        {
            int n = V.Length;
            var A = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) A[i, j] = M[i, j];
                A[i, n] = V[i];
            }
            // 消元
            for (int i = 0; i < n; i++)
            {
                // 主元选择
                int maxRow = i;
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(A[j, i]) > Math.Abs(A[maxRow, i])) maxRow = j;
                if (maxRow != i)
                    for (int k = 0; k <= n; k++)
                    { var tmp = A[i, k]; A[i, k] = A[maxRow, k]; A[maxRow, k] = tmp; }

                // 消元
                for (int j = i + 1; j < n; j++)
                {
                    double f = A[j, i] / A[i, i];
                    for (int k = i; k <= n; k++)
                        A[j, k] -= f * A[i, k];
                }
            }
            // 回代
            var X = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                X[i] = A[i, n];
                for (int j = i + 1; j < n; j++)
                    X[i] -= A[i, j] * X[j];
                X[i] /= A[i, i];
            }
            return X;
        }
    }

    /// <summary>
    /// Class GpsPositionSolverTests.
    /// </summary>
    public class GpsPositionSolverTests
    {
        /// <summary>
        /// 基础测试用例：理想环境四颗卫星定位        
        /// </summary>
        [Test]
        public void SolvePosition_BasicFourSatellites_ShouldConvergeToExpected()
        {
            // 已知接收机真实坐标
            double trueX = 1113194.90793274; // ECEF X，10度经度赤道
            double trueY = 0;
            double trueZ = 0;
            double clockBias = 0;

            var satellites = new List<Satellite>
            {
                new Satellite { X = 15600e3, Y = 7540e3, Z = 20140e3, PRN = 1 },
                new Satellite { X = 18760e3, Y = 2750e3, Z = 18610e3, PRN = 2 },
                new Satellite { X = 17610e3, Y = 14630e3, Z = 13480e3, PRN = 3 },
                new Satellite { X = 19170e3, Y = 610e3,  Z = 18390e3, PRN = 4 }
            };

            // 伪距用真实位置计算
            foreach (var sat in satellites)
            {
                double dx = trueX - sat.X;
                double dy = trueY - sat.Y;
                double dz = trueZ - sat.Z;
                double range = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                sat.PseudoRange = range + GpsPositionSolver.C * clockBias;
                sat.ClockBias = 0;
            }

            var result = GpsPositionSolver.SolvePosition(satellites, new double[] { 0, 0, 0 });

            double error = Math.Sqrt(Math.Pow(result.X - trueX, 2) + Math.Pow(result.Y - trueY, 2) + Math.Pow(result.Z - trueZ, 2));
            Assert.Less(error, 1, $"位置误差应小于1米，当前为{error}");
            Assert.Less(Math.Abs(result.Dt), 1e-6, "钟差应在微秒量级内");
        }

    }
}
