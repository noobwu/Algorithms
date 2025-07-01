// ***********************************************************************
// Assembly         : Noob.Algorithms
// Author           : noob
// Created          : 2025-07-01
//
// Last Modified By : noob
// Last Modified On : 2025-07-01
// ***********************************************************************
// <copyright file="MatchingServiceTests.cs" company="Noob.Algorithms">
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
        public double? MinPrice { get; }

        /// <summary>
        /// 最高可接受价格
        /// </summary>
        /// <value>The maximum price.</value>
        public double? MaxPrice { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Passenger"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="minPrice">The minimum price.</param>
        /// <param name="maxPrice">The maximum price.</param>
        public Passenger(int id, double latitude, double longitude, double? minPrice = null, double? maxPrice = null)
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
    /// Class MatchResult.
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// Gets the passenger.
        /// </summary>
        /// <value>The passenger.</value>
        public Passenger Passenger { get; }
        /// <summary>
        /// Gets the driver.
        /// </summary>
        /// <value>The driver.</value>
        public Driver Driver { get; }
        /// <summary>
        /// Gets the distance.
        /// </summary>
        /// <value>The distance.</value>
        public double Distance { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchResult"/> class.
        /// </summary>
        /// <param name="passenger">The passenger.</param>
        /// <param name="driver">The driver.</param>
        /// <param name="distance">The distance.</param>
        public MatchResult(Passenger passenger, Driver driver, double distance)
        {
            Passenger = passenger;
            Driver = driver;
            Distance = distance;
        }
    }

    /// <summary>
    /// Class GeoUtils.
    /// </summary>
    public static class GeoUtils
    {
        /// <summary>
        /// 默认地球平均半径（公里），用于大圆距离计算。
        /// </summary>
        public const double DefaultEarthRadiusKm = 6371.0;

        /// <summary>
        /// 计算两点间球面距离（Haversine公式，单位：公里，默认保留4位小数）。
        /// 推荐用于 WGS84/GPS 地理坐标。
        /// </summary>
        /// <param name="latitude1">起点纬度（度）</param>
        /// <param name="longitude1">起点经度（度）</param>
        /// <param name="latitude2">终点纬度（度）</param>
        /// <param name="longitude2">终点经度（度）</param>
        /// <param name="sphereRadiusKm">可选：球体半径（公里），默认为地球</param>
        /// <param name="decimalPlaces">结果保留小数位数（默认4位）</param>
        /// <returns>两点球面距离（公里）</returns>
        /// <exception cref="ArgumentOutOfRangeException">参数超出地理有效范围</exception>
        public static double CalculateHaversineDistance(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2,
            double sphereRadiusKm = DefaultEarthRadiusKm,
            int decimalPlaces = 4)
        {
            ValidateLatitude(latitude1, nameof(latitude1));
            ValidateLongitude(longitude1, nameof(longitude1));
            ValidateLatitude(latitude2, nameof(latitude2));
            ValidateLongitude(longitude2, nameof(longitude2));

            if (sphereRadiusKm <= 0)
                throw new ArgumentOutOfRangeException(nameof(sphereRadiusKm), "地球半径必须为正数。");
            if (decimalPlaces < 0)
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "小数位数不能为负。");

            double radLat1 = ToRadians(latitude1);
            double radLat2 = ToRadians(latitude2);
            double deltaLat = radLat2 - radLat1;
            double deltaLon = ToRadians(longitude2 - longitude1);

            double a = Math.Pow(Math.Sin(deltaLat / 2), 2)
                + Math.Cos(radLat1) * Math.Cos(radLat2)
                * Math.Pow(Math.Sin(deltaLon / 2), 2);

            double c = 2 * Math.Asin(Math.Sqrt(a));
            double distance = sphereRadiusKm * c;
            return Math.Round(distance, decimalPlaces);
        }
        /// <summary>
        /// 将角度转换为弧度。
        /// </summary>
        /// <param name="degrees">角度值</param>
        /// <returns>弧度值</returns>
        private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);


        /// <summary>
        /// 检查纬度范围 [-90, 90]。
        /// </summary>
        private static void ValidateLatitude(double latitude, string paramName)
        {
            if (latitude < -90.0 || latitude > 90.0)
                throw new ArgumentOutOfRangeException(paramName, "纬度必须在 -90 到 90 度之间。");
        }

        /// <summary>
        /// 检查经度范围 [-180, 180]。
        /// </summary>
        private static void ValidateLongitude(double longitude, string paramName)
        {
            if (longitude < -180.0 || longitude > 180.0)
                throw new ArgumentOutOfRangeException(paramName, "经度必须在 -180 到 180 度之间。");
        }
    }

    /// <summary>
    /// Interface IMatchingStrategy
    /// </summary>
    public interface IMatchingStrategy
    {
        /// <summary>
        /// Matches the specified passenger.
        /// </summary>
        /// <param name="passenger">The passenger.</param>
        /// <param name="drivers">The drivers.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <returns>MatchResult.</returns>
        MatchResult Match(Passenger passenger, IEnumerable<Driver> drivers, double maxDistance);
    }

    /// <summary>
    /// 最近距离优先策略
    /// </summary>
    public class NearestDriverMatchingStrategy : IMatchingStrategy
    {
        /// <summary>
        /// Matches the specified passenger.
        /// </summary>
        /// <param name="passenger">The passenger.</param>
        /// <param name="drivers">The drivers.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <returns>MatchResult.</returns>
        public MatchResult Match(Passenger passenger, IEnumerable<Driver> drivers,
            double maxDistance)
        {
            Driver nearestDriver = null;
            double minDistance = double.MaxValue;

            foreach (var driver in drivers)
            {
                if (!driver.IsAvailable) continue;

                double distance = GeoUtils.CalculateHaversineDistance(passenger.Latitude, passenger.Longitude, driver.Latitude, driver.Longitude);
                if (distance < minDistance && distance <= maxDistance)
                {
                    minDistance = distance;
                    nearestDriver = driver;
                }
            }

            if (nearestDriver != null)
            {
                nearestDriver.IsAvailable = false;
                return new MatchResult(passenger, nearestDriver, minDistance);
            }

            return null;
        }

    }

    /// <summary>
    /// Class MatchingService.
    /// </summary>
    public class MatchingService
    {
        /// <summary>
        /// The matching strategy
        /// </summary>
        private readonly IMatchingStrategy _matchingStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchingService"/> class.
        /// </summary>
        /// <param name="matchingStrategy">The matching strategy.</param>
        public MatchingService(IMatchingStrategy matchingStrategy)
        {
            _matchingStrategy = matchingStrategy;
        }

        /// <summary>
        /// Matches the passenger to driver.
        /// </summary>
        /// <param name="passenger">The passenger.</param>
        /// <param name="drivers">The drivers.</param>
        /// <param name="maxDistance">The maximum distance.</param>
        /// <returns>MatchResult.</returns>
        public MatchResult MatchPassengerToDriver(Passenger passenger, IEnumerable<Driver> drivers, double maxDistance)
        {
            return _matchingStrategy.Match(passenger, drivers, maxDistance);
        }
    }


    /// <summary>
    /// Defines test class MatchingServiceTests.
    /// </summary>
    [TestFixture]
    public class MatchingServiceTests
    {
        /// <summary>
        /// Defines the test method MatchPassengerToDriver_ShouldReturnNearestDriverWithinMaxDistance.
        /// </summary>
        [Test]
        public void MatchPassengerToDriver_ShouldReturnNearestDriverWithinMaxDistance()
        {
            var passenger = new Passenger(1, 30.0, 120.0);
            var drivers = new List<Driver>
            {
                new Driver(101, 30.001, 120.002),  // 距离更近
                new Driver(102, 29.995, 120.005)
            };

            var matchingService = new MatchingService(new NearestDriverMatchingStrategy());
            var result = matchingService.MatchPassengerToDriver(passenger, drivers, maxDistance: 1.0);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Driver.Id, Is.EqualTo(101));
            Assert.That(result.Passenger.Id, Is.EqualTo(1));
            Assert.That(drivers[0].IsAvailable, Is.False);
        }

        /// <summary>
        /// Defines the test method MatchPassengerToDriver_ShouldReturnNullIfNoDriverWithinMaxDistance.
        /// </summary>
        [Test]
        public void MatchPassengerToDriver_ShouldReturnNullIfNoDriverWithinMaxDistance()
        {
            var passenger = new Passenger(2, 35.0, 120.0);
            var drivers = new List<Driver>
            {
                new Driver(201, 30.0, 120.0)
            };

            var matchingService = new MatchingService(new NearestDriverMatchingStrategy());
            var result = matchingService.MatchPassengerToDriver(passenger, drivers, maxDistance: 1.0);

            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// Defines the test method MatchPassengerToDriver_ShouldOnlyConsiderAvailableDrivers.
        /// </summary>
        [Test]
        public void MatchPassengerToDriver_ShouldOnlyConsiderAvailableDrivers()
        {
            var passenger = new Passenger(3, 30.0, 120.0);
            var drivers = new List<Driver>
            {
                new Driver(301, 30.001, 120.001, isAvailable: false),
                new Driver(302, 30.002, 120.002)
            };

            var matchingService = new MatchingService(new NearestDriverMatchingStrategy());
            var result = matchingService.MatchPassengerToDriver(passenger, drivers, maxDistance: 1.0);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Driver.Id, Is.EqualTo(302));
            Assert.That(drivers[1].IsAvailable, Is.False);
        }
    }

}
