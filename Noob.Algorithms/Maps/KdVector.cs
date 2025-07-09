using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.Maps
{
    /// <summary>
    /// 表示 K 维向量，支持任意维度点的空间索引。
    /// </summary>
    public class KdVector
    {
        /// <summary> 向量的所有维度数据。 </summary>
        public double[] Coordinates { get; }

        /// <summary>
        /// 构造函数，传入所有维度的数值。
        /// </summary>
        /// <param name="coordinates">所有维度数值。</param>
        public KdVector(params double[] coordinates)
        {
            if (coordinates == null || coordinates.Length == 0)
                throw new ArgumentException("维度不能为空", nameof(coordinates));
            Coordinates = coordinates;
        }

        /// <summary>
        /// 获取指定维度的值。
        /// </summary>
        /// <param name="index">维度索引。</param>
        public double this[int index] => Coordinates[index];

        /// <summary>
        /// 向量长度（维度）。
        /// </summary>
        public int Dimension => Coordinates.Length;

        /// <summary>
        /// 欧式距离。
        /// </summary>
        public double DistanceTo(KdVector other)
        {
            if (other.Dimension != Dimension)
                throw new ArgumentException("维度不一致", nameof(other));
            double sum = 0;
            for (int i = 0; i < Dimension; i++)
            {
                double diff = Coordinates[i] - other.Coordinates[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }
    }
}
