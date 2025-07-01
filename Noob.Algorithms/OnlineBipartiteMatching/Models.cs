using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.OnlineBipartiteMatching
{
    /// <summary>
    /// 乘客
    /// </summary>
    public class Passenger
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; }
        /// <summary>
        /// Gets the latitude.
        /// </summary>
        /// <value>The latitude.</value>
        public double Latitude { get; }
        /// <summary>
        /// Gets the longitude.
        /// </summary>
        /// <value>The longitude.</value>
        public double Longitude { get; }

        /// <summary>
        /// 最低可接受价格
        /// </summary>
        /// <value>The minimum price.</value>
        public double MinPrice { get; } = 0;

        /// <summary>
        /// 最高可接受价格
        /// </summary>
        /// <value>The maximum price.</value>
        public double MaxPrice { get; }= double.MaxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Passenger"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="minPrice">The minimum price.</param>
        /// <param name="maxPrice">The maximum price.</param>
        public Passenger(int id, double latitude, double longitude, double minPrice = 0, double maxPrice = double.MaxValue)
        {
            Id = id;
            Latitude = latitude;
            Longitude = longitude;
            MinPrice = minPrice;
            MaxPrice = maxPrice;
        }
    }


    /// <summary>
    /// 司机
    /// </summary>
    public class Driver
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; }
        /// <summary>
        /// Gets the latitude.
        /// </summary>
        /// <value>The latitude.</value>
        public double Latitude { get; }
        /// <summary>
        /// Gets the longitude.
        /// </summary>
        /// <value>The longitude.</value>
        public double Longitude { get; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is available.
        /// </summary>
        /// <value><c>true</c> if this instance is available; otherwise, <c>false</c>.</value>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 当前订单司机期望价格
        /// </summary>
        /// <value>The base price.</value>
        public double BasePrice { get; set; }

        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        /// <value>The rating.</value>
        public double Rating { get; set; }

        /// <summary>
        /// Gets or sets the acceptance rate.
        /// </summary>
        /// <value>The acceptance rate.</value>
        public double AcceptanceRate { get; set; }

        /// <summary>
        /// Gets or sets the idle minutes.
        /// </summary>
        /// <value>The idle minutes.</value>
        public double IdleMinutes { get; set; }

        // 可进一步扩展：司机历史偏好/对高峰期打赏敏感度等

        /// <summary>
        /// Initializes a new instance of the <see cref="Driver"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="isAvailable">if set to <c>true</c> [is available].</param>
        public Driver(int id, double latitude, double longitude, bool isAvailable = true)
        {
            Id = id;
            Latitude = latitude;
            Longitude = longitude;
            IsAvailable = isAvailable;
        }
    }

}
