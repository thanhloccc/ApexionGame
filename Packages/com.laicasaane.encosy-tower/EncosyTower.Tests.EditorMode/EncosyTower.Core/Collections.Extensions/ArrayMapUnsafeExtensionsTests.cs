using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using MapAPI = EncosyTower.Collections.Extensions.ArrayMapUnsafeExtensions;
using MapReadOnlyAPI = EncosyTower.Collections.Extensions.ArrayMapUnsafeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections.Extensions
{
    public partial class ArrayMapUnsafeExtensionsTests
    {
        [Test]
        public void GetValues_MutableAndReadOnlyViewsReflectMapStorage()
        {
            using var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            map.Add(1, 10);
            map.Add(2, 20);

            var values = MapAPI.GetValues(in map);
            var readOnly = map.AsReadOnly();
            var readOnlyValues = MapReadOnlyAPI.GetValues(in readOnly);

            Assert.AreEqual(map.Count, values.Length);
            Assert.AreEqual(map.Count, readOnlyValues.Length);
            Assert.AreEqual(10, readOnlyValues[0]);

            values[0] = 99;

            Assert.AreEqual(99, map[1]);
        }

        [Test]
        public void GetOrAdd_StructBuildersRunOnlyForMissingKeys()
        {
            var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            var builder = new IntBuilder(30);

            ref var added = ref MapAPI.GetOrAdd(ref map, 1, ref builder);
            ref var existing = ref MapAPI.GetOrAdd(ref map, 1, ref builder);

            Assert.AreEqual(30, added);
            Assert.AreEqual(30, existing);
            Assert.AreEqual(1, builder.calls);

            var parameter = 40;
            ref var parameterAdded = ref MapAPI.GetOrAdd(
                  ref map
                , 2
                , ref builder
                , ref parameter
            );
            parameter = 50;
            ref var parameterExisting = ref MapAPI.GetOrAdd(
                  ref map
                , 2
                , ref builder
                , ref parameter
            );

            Assert.AreEqual(40, parameterAdded);
            Assert.AreEqual(40, parameterExisting);
            Assert.AreEqual(2, builder.calls);

            map.Dispose();
        }

        [Test]
        public void GetOrAdd_DelegateBuildersRunOnlyForMissingKeys()
        {
            var map = new ArrayMapUnsafe<int, int>(4, Allocator.Temp);
            var buildCount = 0;

            try
            {
                ref var added = ref MapAPI.GetOrAdd(ref map, 1, () =>
                {
                    buildCount++;
                    return 30;
                });
                ref var existing = ref MapAPI.GetOrAdd(ref map, 1, () =>
                {
                    buildCount++;
                    return 31;
                });
                var parameter = 40;
                ref var parameterAdded = ref MapAPI.GetOrAdd(
                      ref map
                    , 2
                    , static (ref int state) => state
                    , ref parameter
                );
                parameter = 50;
                ref var parameterExisting = ref MapAPI.GetOrAdd(
                      ref map
                    , 2
                    , static (ref int state) => state
                    , ref parameter
                );

                Assert.AreEqual(30, added);
                Assert.AreEqual(30, existing);
                Assert.AreEqual(1, buildCount);
                Assert.AreEqual(40, parameterAdded);
                Assert.AreEqual(40, parameterExisting);
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void RecycleOrAdd_DelegateOverloadsRecycleOrBuildValues()
        {
            var plainMap = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);
            plainMap.Add(1, 10);
            plainMap.Recycle();

            ref var recycled = ref MapAPI.RecycleOrAdd(
                  ref plainMap
                , 2
                , static () => 30
                , static (ref int value) => value = 20
                , static (ref int value) => value == 10
            );
            ref var built = ref MapAPI.RecycleOrAdd(
                  ref plainMap
                , 3
                , static () => 30
                , static (ref int value) => value = 20
                , static (ref int value) => value == 10
            );

            Assert.AreEqual(20, recycled);
            Assert.AreEqual(30, built);

            var parameterMap = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);
            parameterMap.Add(1, 10);
            parameterMap.Recycle();
            var parameter = 40;

            ref var parameterRecycled = ref MapAPI.RecycleOrAdd(
                  ref parameterMap
                , 2
                , static (ref int state) => state
                , static (ref int value, ref int state) => value = state
                , static (ref int value) => value == 10
                , ref parameter
            );
            parameter = 50;
            ref var parameterBuilt = ref MapAPI.RecycleOrAdd(
                  ref parameterMap
                , 3
                , static (ref int state) => state
                , static (ref int value, ref int state) => value = state
                , static (ref int value) => value == 10
                , ref parameter
            );

            Assert.AreEqual(40, parameterRecycled);
            Assert.AreEqual(50, parameterBuilt);

            plainMap.Dispose();
            parameterMap.Dispose();
        }

        [Test]
        public void RecycleOrAdd_StructOverloadsRecycleOrBuildValues()
        {
            var plainMap = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);
            plainMap.Add(1, 10);
            plainMap.Recycle();
            var builder = new IntBuilder(30);
            var recycler = new IntRecycler(20);
            var predicate = new IntPredicate(10);

            ref var recycled = ref MapAPI.RecycleOrAdd(
                  ref plainMap
                , 2
                , ref builder
                , ref recycler
                , ref predicate
            );
            ref var built = ref MapAPI.RecycleOrAdd(
                  ref plainMap
                , 3
                , ref builder
                , ref recycler
                , ref predicate
            );

            Assert.AreEqual(20, recycled);
            Assert.AreEqual(30, built);
            Assert.AreEqual(1, builder.calls);
            Assert.AreEqual(1, recycler.calls);

            var parameterMap = new ArrayMapUnsafe<int, int>(2, Allocator.Temp);
            parameterMap.Add(1, 10);
            parameterMap.Recycle();
            builder = new IntBuilder(-1);
            recycler = new IntRecycler(-1);
            predicate = new IntPredicate(10);
            var parameter = 40;

            ref var parameterRecycled = ref MapAPI.RecycleOrAdd(
                  ref parameterMap
                , 2
                , ref builder
                , ref recycler
                , ref predicate
                , ref parameter
            );
            parameter = 50;
            ref var parameterBuilt = ref MapAPI.RecycleOrAdd(
                  ref parameterMap
                , 3
                , ref builder
                , ref recycler
                , ref predicate
                , ref parameter
            );

            Assert.AreEqual(40, parameterRecycled);
            Assert.AreEqual(50, parameterBuilt);
            Assert.AreEqual(1, builder.calls);
            Assert.AreEqual(1, recycler.calls);

            plainMap.Dispose();
            parameterMap.Dispose();
        }
    }
}
