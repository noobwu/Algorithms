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


    /// <summary>
    /// Class Doctor.
    /// </summary>
    public class Doctor
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the preference.
        /// </summary>
        /// <value>The preference.</value>
        public PreferenceSim Preference { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Doctor"/> class.
        /// </summary>
        public Doctor() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Doctor"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        public Doctor(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    /// <summary>
    /// 医生排班信息
    /// </summary>
    public class Shift
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the distance km.
        /// </summary>
        /// <value>The distance km.</value>
        public double DistanceKm { get; set; }

        /// <summary>
        /// Gets or sets the estimated profit.
        /// </summary>
        /// <value>The estimated profit.</value>
        public double EstimatedProfit { get; set; }

        /// <summary>
        /// Gets or sets the required skill.
        /// </summary>
        /// <value>The required skill.</value>
        public string RequiredSkill { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shift"/> class.
        /// </summary>
        public Shift() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shift"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        public Shift(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    /// <summary>
    /// Class PreferenceSim.
    /// </summary>
    public class PreferenceSim
    {
        /// <summary>
        /// The liked shifts
        /// </summary>
        private readonly HashSet<int> likedShifts;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreferenceSim"/> class.
        /// </summary>
        /// <param name="liked">The liked.</param>
        public PreferenceSim(params int[] liked) => likedShifts = new HashSet<int>(liked);

        /// <summary>
        /// Gets the score.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>System.Double.</returns>
        public double GetScore(Shift s) => likedShifts.Contains(s.Id) ? 1.0 : 0.0;
    }
}
