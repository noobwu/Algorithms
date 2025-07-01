using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noob.Algorithms.OnlineBipartiteMatching
{
    /// <summary>
    /// Class Doctor.
    /// </summary>
    public class Doctor
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

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
        public int Id { get; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

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
    /// Interface IEligibilityProvider
    /// </summary>
    public interface IEligibilityProvider
    {      
        /// <summary>
        /// 判断某医生是否可排该班次
        /// </summary>
        /// <param name="doctor">The doctor.</param>
        /// <param name="shift">The shift.</param>
        /// <returns><c>true</c> if the specified doctor is eligible; otherwise, <c>false</c>.</returns>
        bool IsEligible(Doctor doctor, Shift shift);
    }

    /// <summary>
    /// Class EligibilityMatrixProvider.
    /// Implements the <see cref="Noob.Algorithms.OnlineBipartiteMatching.IEligibilityProvider" />
    /// </summary>
    /// <seealso cref="Noob.Algorithms.OnlineBipartiteMatching.IEligibilityProvider" />
    public class EligibilityMatrixProvider : IEligibilityProvider
    {
        /// <summary>
        /// The eligible pairs
        /// </summary>
        private readonly HashSet<(int, int)> _eligiblePairs;

        /// <summary>
        /// Initializes a new instance of the <see cref="EligibilityMatrixProvider"/> class.
        /// </summary>
        /// <param name="eligiblePairs">The eligible pairs.</param>
        public EligibilityMatrixProvider(IEnumerable<(int doctorId, int shiftId)> eligiblePairs)
        {
            _eligiblePairs = new HashSet<(int, int)>(eligiblePairs);
        }

        /// <summary>
        /// 判断某医生是否可排该班次
        /// </summary>
        /// <param name="doctor">The doctor.</param>
        /// <param name="shift">The shift.</param>
        /// <returns><c>true</c> if the specified doctor is eligible; otherwise, <c>false</c>.</returns>
        public bool IsEligible(Doctor doctor, Shift shift) =>
            _eligiblePairs.Contains((doctor.Id, shift.Id));
    }

    /// <summary>
    /// Interface IScheduleMatcher
    /// </summary>
    public interface IScheduleMatcher
    {
        /// <summary>
        /// Finds the maximum matching.
        /// </summary>
        /// <param name="doctors">The doctors.</param>
        /// <param name="shifts">The shifts.</param>
        /// <param name="eligibilityProvider">The eligibility provider.</param>
        /// <returns>IDictionary&lt;Shift, Doctor&gt;.</returns>
        IDictionary<Shift, Doctor> FindMaximumMatching(
            IEnumerable<Doctor> doctors,
            IEnumerable<Shift> shifts,
            IEligibilityProvider eligibilityProvider);
    }

    /// <summary>
    /// Class MaximumMatchingMatcher.
    /// Implements the <see cref="Noob.Algorithms.OnlineBipartiteMatching.IScheduleMatcher" />
    /// </summary>
    /// <seealso cref="Noob.Algorithms.OnlineBipartiteMatching.IScheduleMatcher" />
    public class MaximumMatchingMatcher : IScheduleMatcher
    {
        /// <summary>
        /// Finds the maximum matching.
        /// </summary>
        /// <param name="doctors">The doctors.</param>
        /// <param name="shifts">The shifts.</param>
        /// <param name="eligibilityProvider">The eligibility provider.</param>
        /// <returns>IDictionary&lt;Shift, Doctor&gt;.</returns>
        public IDictionary<Shift, Doctor> FindMaximumMatching(
            IEnumerable<Doctor> doctors,
            IEnumerable<Shift> shifts,
            IEligibilityProvider eligibilityProvider)
        {
            var doctorList = doctors.ToList();
            var shiftList = shifts.ToList();
            var shiftCount = shiftList.Count;
            var match = new Doctor[shiftCount];

            // 邻接表
            var adj = new List<int>[doctorList.Count];
            for (int i = 0; i < doctorList.Count; i++)
            {
                adj[i] = new List<int>();
                for (int j = 0; j < shiftList.Count; j++)
                {
                    if (eligibilityProvider.IsEligible(doctorList[i], shiftList[j]))
                        adj[i].Add(j);
                }
            }

            // DFS（匈牙利算法核心）
            bool Dfs(int doctorIdx, bool[] visited)
            {
                foreach (var shiftIdx in adj[doctorIdx])
                {
                    if (visited[shiftIdx]) continue;
                    visited[shiftIdx] = true;
                    if (match[shiftIdx] == null || Dfs(doctorList.IndexOf(match[shiftIdx]), visited))
                    {
                        match[shiftIdx] = doctorList[doctorIdx];
                        return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < doctorList.Count; i++)
                Dfs(i, new bool[shiftCount]);

            var result = new Dictionary<Shift, Doctor>();
            for (int j = 0; j < shiftCount; j++)
                if (match[j] != null)
                    result[shiftList[j]] = match[j];
            return result;
        }
    }


    /// <summary>
    /// Class ScheduleService.
    /// </summary>
    public class ScheduleService
    {
        /// <summary>
        /// The matcher
        /// </summary>
        private readonly IScheduleMatcher _matcher;
        /// <summary>
        /// The eligibility provider
        /// </summary>
        private readonly IEligibilityProvider _eligibilityProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleService"/> class.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="eligibilityProvider">The eligibility provider.</param>
        public ScheduleService(IScheduleMatcher matcher, IEligibilityProvider eligibilityProvider)
        {
            _matcher = matcher;
            _eligibilityProvider = eligibilityProvider;
        }

        /// <summary>
        /// Assigns the doctors to shifts.
        /// </summary>
        /// <param name="doctors">The doctors.</param>
        /// <param name="shifts">The shifts.</param>
        /// <returns>IDictionary&lt;Shift, Doctor&gt;.</returns>
        public IDictionary<Shift, Doctor> AssignDoctorsToShifts(IEnumerable<Doctor> doctors, IEnumerable<Shift> shifts)
        {
            return _matcher.FindMaximumMatching(doctors, shifts, _eligibilityProvider);
        }
    }

    /// <summary>
    /// Defines test class ScheduleServiceTests.
    /// </summary>
    [TestFixture]
    public class ScheduleServiceTests
    {
        /// <summary>
        /// Defines the test method AssignDoctorsToShifts_MaxMatching_ReturnsExpectedResult.
        /// </summary>
        [Test]
        public void AssignDoctorsToShifts_MaxMatching_ReturnsExpectedResult()
        {
            var doctors = new List<Doctor>
            {
                new Doctor(1, "Dr. Alice"),
                new Doctor(2, "Dr. Bob"),
                new Doctor(3, "Dr. Carol"),
            };

            var shifts = new List<Shift>
            {
                new Shift(1, "Mon"),
                new Shift(2, "Tue"),
                new Shift(3, "Wed"),
            };

            var eligiblePairs = new[]
            {
                (1,1), (1,2), (2,2), (2,3), (3,3)
            };

            var eligibilityProvider = new EligibilityMatrixProvider(eligiblePairs);
            var matcher = new MaximumMatchingMatcher();
            var service = new ScheduleService(matcher, eligibilityProvider);

            var result = service.AssignDoctorsToShifts(doctors, shifts);

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[shifts[0]].Name, Is.EqualTo("Dr. Alice"));
            Assert.That(result[shifts[1]].Name, Is.EqualTo("Dr. Bob").Or.EqualTo("Dr. Alice"));
            Assert.That(result[shifts[2]].Name, Is.EqualTo("Dr. Carol").Or.EqualTo("Dr. Bob"));
        }

        /// <summary>
        /// Defines the test method AssignDoctorsToShifts_PartialMatching_ReturnsExpectedResult.
        /// </summary>
        [Test]
        public void AssignDoctorsToShifts_PartialMatching_ReturnsExpectedResult()
        {
            var doctors = new List<Doctor>
            {
                new Doctor(1, "Dr. Alice"),
                new Doctor(2, "Dr. Bob")
            };

            var shifts = new List<Shift>
            {
                new Shift(1, "Mon"),
                new Shift(2, "Tue"),
            };

            var eligiblePairs = new[]
            {
                (1,1), (2,2)
            };

            var eligibilityProvider = new EligibilityMatrixProvider(eligiblePairs);
            var matcher = new MaximumMatchingMatcher();
            var service = new ScheduleService(matcher, eligibilityProvider);

            var result = service.AssignDoctorsToShifts(doctors, shifts);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[shifts[0]].Name, Is.EqualTo("Dr. Alice"));
            Assert.That(result[shifts[1]].Name, Is.EqualTo("Dr. Bob"));
        }
    }

}
