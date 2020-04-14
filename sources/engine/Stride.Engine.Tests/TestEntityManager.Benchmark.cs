// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;

namespace Stride.Engine.Tests
{
    public partial class TestEntityManager
    {
        [Fact]
        public void Benchmark()
        {
            const int TestCount = 5;
            const int TestEntityCount = 10000;

            long totalTime = 0;
            long stepTime = 0;
            var clock = Stopwatch.StartNew();
            Console.WriteLine($"Test1 -> [Add {TestEntityCount} entities + 10 custom components] x {TestCount} times");
            Console.WriteLine($"Test2 -> [Add {TestEntityCount} entities], [Add 10 custom component, Remove 10 custom component] x {TestCount} times)");

            DumpGC($"Start Test1 - ");
            for (int j = 0; j < TestCount; j++)
            {
                var registry = new ServiceRegistry();
                var entityManager = new CustomEntityManager(registry);

                clock.Restart();
                for (int i = 0; i < TestEntityCount; i++)
                {
                    var entity = new Entity
                    {
                        new BenchComponent1(),
                        new BenchComponent2(),
                        new BenchComponent3(),
                        new BenchComponent4(),
                        new BenchComponent5(),
                        new BenchComponent6(),
                        new BenchComponent7(),
                        new BenchComponent8(),
                        new BenchComponent9(),
                        new BenchComponent10(),
                    };

                    entityManager.Add(entity);
                }
                var elapsed = clock.ElapsedMilliseconds;
                stepTime += elapsed;
                DumpGC($"\t[{j}] Elapsed: {elapsed}ms ");
            }
            DumpGC($"End - Elapsed {stepTime}ms ");
            totalTime += stepTime;
            stepTime = 0;

            Console.WriteLine();

            DumpGC($"Start Test2 - ");
            {
                var registry = new ServiceRegistry();
                var entityManager = new CustomEntityManager(registry);

                for (int i = 0; i < TestEntityCount; i++)
                {
                    var entity = new Entity();
                    entityManager.Add(entity);
                }

                for (int j = 0; j < TestCount; j++)
                {
                    clock.Restart();
                    foreach (var entity in entityManager)
                    {
                        entity.Add(new BenchComponent1());
                        entity.Add(new BenchComponent2());
                        entity.Add(new BenchComponent3());
                        entity.Add(new BenchComponent4());
                        entity.Add(new BenchComponent5());
                        entity.Add(new BenchComponent6());
                        entity.Add(new BenchComponent7());
                        entity.Add(new BenchComponent8());
                        entity.Add(new BenchComponent9());
                        entity.Add(new BenchComponent10());
                    }
                    var elapsedAdd = clock.ElapsedMilliseconds;
                    stepTime += elapsedAdd;
                    clock.Restart();

                    foreach (var entity in entityManager)
                    {
                        entity.Remove<BenchComponent1>();
                        entity.Remove<BenchComponent2>();
                        entity.Remove<BenchComponent3>();
                        entity.Remove<BenchComponent4>();
                        entity.Remove<BenchComponent5>();
                        entity.Remove<BenchComponent6>();
                        entity.Remove<BenchComponent7>();
                        entity.Remove<BenchComponent8>();
                        entity.Remove<BenchComponent9>();
                        entity.Remove<BenchComponent10>();
                    }

                    var elapsedRemove = clock.ElapsedMilliseconds;
                    stepTime += elapsedRemove;
                    DumpGC($"\t[{j}] ElapsedAdd: {elapsedAdd} ElapsedRemove: {elapsedRemove} ");
                }
            }
            DumpGC($"End - Elapsed {stepTime}ms ");
            totalTime += stepTime;

            // Only perform this assert on Windows
            if (Platform.Type == PlatformType.Windows)
            {
                Assert.True(totalTime < 3000, "This test should run in less than 3000ms");
            }

            Console.WriteLine($"Total Time: {totalTime}ms");
        }
        internal static void DumpGC(string text)
        {
            var totalMemory = GC.GetTotalMemory(false);
            var collect0 = GC.CollectionCount(0);
            var collect1 = GC.CollectionCount(1);
            var collect2 = GC.CollectionCount(2);

            Console.WriteLine($"{text}Memory: {totalMemory} GC0: {collect0} GC1: {collect1} GC2: {collect2}");
        }
    }


    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent1 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent1>
        {
        }
    }
    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent2 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent2>
        {
        }
    }
    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent3 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent3>
        {
        }
    }
    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent4 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent4>
        {
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent5 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent5>
        {
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent6 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent6>
        {
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent7 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent7>
        {
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent8 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent8>
        {
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent9 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent9>
        {
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(Processor))]
    public sealed class BenchComponent10 : CustomEntityComponentBase
    {
        public class Processor : CustomEntityComponentProcessor<BenchComponent10>
        {
        }
    }
}
