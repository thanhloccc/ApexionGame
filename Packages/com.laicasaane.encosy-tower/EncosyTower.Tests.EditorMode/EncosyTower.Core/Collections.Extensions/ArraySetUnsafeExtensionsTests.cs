using EncosyTower.Collections.Unsafe;
using NUnit.Framework;
using Unity.Collections;

using SetAPI = EncosyTower.Collections.Extensions.ArraySetUnsafeExtensions;
using SetReadOnlyAPI = EncosyTower.Collections.Extensions.ArraySetUnsafeReadOnlyExtensions;

namespace EncosyTower.Tests.Core.Collections.Extensions
{
    public partial class ArraySetUnsafeExtensionsTests
    {
        [Test]
        public void GetItems_MutableAndReadOnlyViewsReflectSetStorage()
        {
            using var set = new ArraySetUnsafe<int>(4, Allocator.Temp);
            set.Add(10);
            set.Add(20);

            var items = SetAPI.GetItems(in set);
            var readOnly = set.AsReadOnly();
            var readOnlyItems = SetReadOnlyAPI.GetItems(in readOnly);

            Assert.AreEqual(set.Count, items.Length);
            Assert.AreEqual(set.Count, readOnlyItems.Length);
            Assert.AreEqual(10, readOnlyItems[0]);

            items[0] = 99;

            Assert.AreEqual(99, set.Items[0]);
        }

        [Test]
        public void GetOrAdd_DelegateOverloadsBuildMissingValues()
        {
            var plainSet = new ArraySetUnsafe<int>(4, Allocator.Temp);

            ref var added = ref SetAPI.GetOrAdd(ref plainSet, 10, static () => 10);
            ref var existing = ref SetAPI.GetOrAdd(ref plainSet, 10, static () => 99);

            Assert.AreEqual(10, added);
            Assert.AreEqual(10, existing);

            var parameterSet = new ArraySetUnsafe<int>(4, Allocator.Temp);
            var parameter = 20;

            ref var parameterAdded = ref SetAPI.GetOrAdd(
                  ref parameterSet
                , 20
                , static (ref int state) => state
                , ref parameter
            );
            parameter = 99;
            ref var parameterExisting = ref SetAPI.GetOrAdd(
                  ref parameterSet
                , 20
                , static (ref int state) => state
                , ref parameter
            );

            Assert.AreEqual(20, parameterAdded);
            Assert.AreEqual(20, parameterExisting);

            plainSet.Dispose();
            parameterSet.Dispose();
        }

        [Test]
        public void GetOrAdd_StructOverloadsBuildMissingValuesOnce()
        {
            var set = new ArraySetUnsafe<int>(4, Allocator.Temp);
            var builder = new IntBuilder(10);

            ref var added = ref SetAPI.GetOrAdd(ref set, 10, ref builder);
            ref var existing = ref SetAPI.GetOrAdd(ref set, 10, ref builder);

            Assert.AreEqual(10, added);
            Assert.AreEqual(10, existing);
            Assert.AreEqual(1, builder.calls);

            var parameter = 20;
            ref var parameterAdded = ref SetAPI.GetOrAdd(
                  ref set
                , 20
                , ref builder
                , ref parameter
            );
            parameter = 99;
            ref var parameterExisting = ref SetAPI.GetOrAdd(
                  ref set
                , 20
                , ref builder
                , ref parameter
            );

            Assert.AreEqual(20, parameterAdded);
            Assert.AreEqual(20, parameterExisting);
            Assert.AreEqual(2, builder.calls);

            set.Dispose();
        }

        [Test]
        public void RecycleOrAdd_DelegateOverloadsRecycleOrBuildValues()
        {
            var plainSet = new ArraySetUnsafe<int>(2, Allocator.Temp);
            plainSet.Add(10);
            plainSet.Clear();

            ref var recycled = ref SetAPI.RecycleOrAdd(
                  ref plainSet
                , 20
                , static () => 30
                , static (ref int value) => value = 20
                , static (ref int value) => value == 10
            );
            ref var built = ref SetAPI.RecycleOrAdd(
                  ref plainSet
                , 30
                , static () => 30
                , static (ref int value) => value = 20
                , static (ref int value) => value == 10
            );

            Assert.AreEqual(20, recycled);
            Assert.AreEqual(30, built);

            var parameterSet = new ArraySetUnsafe<int>(2, Allocator.Temp);
            parameterSet.Add(10);
            parameterSet.Clear();
            var parameter = 40;

            ref var parameterRecycled = ref SetAPI.RecycleOrAdd(
                  ref parameterSet
                , 40
                , static (ref int state) => state
                , static (ref int value, ref int state) => value = state
                , static (ref int value) => value == 10
                , ref parameter
            );
            parameter = 50;
            ref var parameterBuilt = ref SetAPI.RecycleOrAdd(
                  ref parameterSet
                , 50
                , static (ref int state) => state
                , static (ref int value, ref int state) => value = state
                , static (ref int value) => value == 10
                , ref parameter
            );

            Assert.AreEqual(40, parameterRecycled);
            Assert.AreEqual(50, parameterBuilt);

            plainSet.Dispose();
            parameterSet.Dispose();
        }

        [Test]
        public void RecycleOrAdd_StructOverloadsRecycleOrBuildValues()
        {
            var plainSet = new ArraySetUnsafe<int>(2, Allocator.Temp);
            plainSet.Add(10);
            plainSet.Clear();
            var builder = new IntBuilder(30);
            var recycler = new IntRecycler(20);
            var predicate = new IntPredicate(10);

            ref var recycled = ref SetAPI.RecycleOrAdd(
                  ref plainSet
                , 20
                , ref builder
                , ref recycler
                , ref predicate
            );
            ref var built = ref SetAPI.RecycleOrAdd(
                  ref plainSet
                , 30
                , ref builder
                , ref recycler
                , ref predicate
            );

            Assert.AreEqual(20, recycled);
            Assert.AreEqual(30, built);
            Assert.AreEqual(1, builder.calls);
            Assert.AreEqual(1, recycler.calls);

            var parameterSet = new ArraySetUnsafe<int>(2, Allocator.Temp);
            parameterSet.Add(10);
            parameterSet.Clear();
            builder = new IntBuilder(-1);
            recycler = new IntRecycler(-1);
            predicate = new IntPredicate(10);
            var parameter = 40;

            ref var parameterRecycled = ref SetAPI.RecycleOrAdd(
                  ref parameterSet
                , 40
                , ref builder
                , ref recycler
                , ref predicate
                , ref parameter
            );
            parameter = 50;
            ref var parameterBuilt = ref SetAPI.RecycleOrAdd(
                  ref parameterSet
                , 50
                , ref builder
                , ref recycler
                , ref predicate
                , ref parameter
            );

            Assert.AreEqual(40, parameterRecycled);
            Assert.AreEqual(50, parameterBuilt);
            Assert.AreEqual(1, builder.calls);
            Assert.AreEqual(1, recycler.calls);

            plainSet.Dispose();
            parameterSet.Dispose();
        }
    }
}
