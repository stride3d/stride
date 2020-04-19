using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Stride.Metrics.ServerApp.Models
{
    /// <summary>
    /// This class is used to fill the database with some "random" data to simulate users interaction.
    /// </summary>
    internal class MetricDbTest
    {
        public static void Fill(MetricDbContext db)
        {
            var fromTime = new DateTime(2015, 01, 3);
            var toTime = DateTime.Now;

            var installGenerators = new List<PseudoInstallGenerator>()
            {
                new PseudoInstallGenerator(new DateTime(2015, 01, 20), 1, 3, "1.0.0")
                {
                    new PseudoMetricEventGenerator(0.7, 1, -1, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(3)),
                    new PseudoMetricEventGenerator(0.9, 1, -0.2, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(20)),
                    new PseudoMetricEventGenerator(1.0, 2, -0.05, TimeSpan.FromHours(2), TimeSpan.FromHours(1)),
                },

                new PseudoInstallGenerator(new DateTime(2015, 03, 20), 1, 5, "1.1.0")
                {
                    new PseudoMetricEventGenerator(0.5, 1, -1, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(3)),
                    new PseudoMetricEventGenerator(0.7, 1, -0.2, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(20)),
                    new PseudoMetricEventGenerator(0.9, 1, -0.01, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(20)),
                    new PseudoMetricEventGenerator(1.0, 2, -0.005, TimeSpan.FromHours(3), TimeSpan.FromHours(1)),
                },

                new PseudoInstallGenerator(toTime, 1, 10, "1.2.0")
                {
                    new PseudoMetricEventGenerator(0.3, 1, -1, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(3)),
                    new PseudoMetricEventGenerator(0.5, 1, -0.1, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(20)),
                    new PseudoMetricEventGenerator(0.7, 1, -0.01, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(20)),
                    new PseudoMetricEventGenerator(1.0, 2, -0.005, TimeSpan.FromHours(3), TimeSpan.FromHours(1)),
                },
            };

            var currentTime = fromTime;
            var random = new Random(0);

            var userInstalls = new List<PseudoInstall>();

            while (currentTime < toTime)
            {
                var generator = installGenerators.FirstOrDefault(t => currentTime < t.Until);
                if (generator == null)
                {
                    break;
                }

                var installCount = random.Next(generator.InstallCountMin, generator.InstallCountMax);

                for (int i = 0; i < installCount; i++)
                {
                    var p = random.NextDouble();
                    foreach (var eventGenerators in generator.MetricEventGenerators)
                    {
                        if (p <= eventGenerators.Percentage)
                        {
                            eventGenerators.InstallGenerator = generator;
                            var pseudoInstall =
                                new PseudoInstall(
                                    new MetricInstall(Guid.NewGuid())
                                    {
                                        Created = currentTime + TimeSpan.FromHours(6 + i*10/installCount)
                                    },
                                    eventGenerators)
                                {
                                    NumberPerDay = eventGenerators.NumberPerDay,
                                };
                                
                            // Save the install to the database
                            db.Installs.Add(pseudoInstall.MetricInstall);

                            // Add our new pseudo install
                            userInstalls.Add(pseudoInstall);
                            break;
                        }
                    }
                }

                db.SaveChanges();

                int eventCount = 0;
                // Generate user events
                for (int i = 0; i < userInstalls.Count; i++)
                {
                    var pseudoInstall = userInstalls[i];

                    // Saving and storing metrics per user install every day. This is not correct as we should ideally save them ordered by timestamp between all users
                    // but for this test, we simplify this.
                    for (int eventId = 0; eventId < pseudoInstall.NumberPerDay; eventId++)
                    {
                        var metricEvent = new NewMetricMessage()
                        {
                            ApplicationId = CommonApps.StrideEditorAppId.Guid,
                            InstallId = pseudoInstall.MetricInstall.InstallGuid,
                            SessionId = eventId,
                            EventId = eventId,
                            MetricId = CommonMetrics.OpenApplication.Guid,
                            Value = pseudoInstall.EventGenerator.InstallGenerator.Version
                        };

                        var time = currentTime + TimeSpan.FromHours(7 + eventId*10/pseudoInstall.NumberPerDay);
                        db.SaveNewMetric(metricEvent, pseudoInstall.IPAddress, time, true);

                        metricEvent = new NewMetricMessage()
                        {
                            ApplicationId = CommonApps.StrideEditorAppId.Guid,
                            InstallId = pseudoInstall.MetricInstall.InstallGuid,
                            SessionId = eventId,
                            EventId = eventId,
                            MetricId = CommonMetrics.CloseApplication.Guid,
                            Value = string.Empty
                        };
                        time += pseudoInstall.EventGenerator.Duration + TimeSpan.FromSeconds(random.NextDouble()*pseudoInstall.EventGenerator.DurationAddDelta.TotalSeconds);
                        db.SaveNewMetric(metricEvent, pseudoInstall.IPAddress, time, true);

                        eventCount += 2;
                    }

                    // Decrease the installs
                    pseudoInstall.NumberPerDay += pseudoInstall.EventGenerator.DecreaseNumberPerDay;
                    if (pseudoInstall.NumberPerDay <= 0.0)
                    {
                        userInstalls.RemoveAt(i);
                        i--;
                    }
                }

                db.SaveChanges();

                Debug.WriteLine("{0}: Users: {1} Events {2}", currentTime, userInstalls.Count, eventCount);

                // Add one day
                currentTime += TimeSpan.FromDays(1);
            }
        }

        private class PseudoInstallGenerator : IEnumerable<PseudoMetricEventGenerator>
        {
            public PseudoInstallGenerator(DateTime until, int installCountMin, int installCountMax, string version)
            {
                Until = until;
                InstallCountMin = installCountMin;
                InstallCountMax = installCountMax;
                MetricEventGenerators = new List<PseudoMetricEventGenerator>();
                Version = version;
            }

            public readonly DateTime Until;

            public readonly int InstallCountMin;

            public readonly int InstallCountMax;

            public readonly string Version;

            public List<PseudoMetricEventGenerator> MetricEventGenerators { get; private set; }

            public void Add(PseudoMetricEventGenerator metricEventGenerator)
            {
                if (metricEventGenerator == null) throw new ArgumentNullException("metricEventGenerator");
                MetricEventGenerators.Add(metricEventGenerator);
            }


            public IEnumerator<PseudoMetricEventGenerator> GetEnumerator()
            {
                return MetricEventGenerators.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable) MetricEventGenerators).GetEnumerator();
            }
        }


        private class PseudoInstall
        {
            public PseudoInstall(MetricInstall metricInstall, PseudoMetricEventGenerator eventGenerator)
            {
                MetricInstall = metricInstall;
                EventGenerator = eventGenerator;
                IPAddress = RandomIpGenerator.GetRandomIp();
            }


            public readonly MetricInstall MetricInstall;

            public readonly PseudoMetricEventGenerator EventGenerator;

            public double NumberPerDay;

            public readonly string IPAddress;

            public static class RandomIpGenerator
            {
                private static readonly Random RandomIp = new Random(0);
                public static string GetRandomIp()
                {
                    return string.Format("{0}.{1}.{2}.{3}", RandomIp.Next(0, 255), RandomIp.Next(0, 255), RandomIp.Next(0, 255), RandomIp.Next(0, 255));
                }
            }
        }

        private class PseudoMetricEventGenerator
        {
            public PseudoMetricEventGenerator(double percentage, int numberPerDay, double decreaseNumberPerDay, TimeSpan duration, TimeSpan durationAddDelta)
            {
                Percentage = percentage;
                NumberPerDay = numberPerDay;
                DecreaseNumberPerDay = decreaseNumberPerDay;
                Duration = duration;
                DurationAddDelta = durationAddDelta;
            }

            public readonly double Percentage;

            public readonly int NumberPerDay;

            public readonly double DecreaseNumberPerDay;

            public readonly TimeSpan Duration;

            public readonly TimeSpan DurationAddDelta;

            public PseudoInstallGenerator InstallGenerator;
        }
    }
}