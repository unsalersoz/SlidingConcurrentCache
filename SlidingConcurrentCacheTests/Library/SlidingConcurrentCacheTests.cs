using System;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using NUnit.Framework;
using SlidingConcurrentCache.Interface;
using SlidingConcurrentCache.Library;

namespace SlidingConcurrentCacheTests.Library
{
    [TestFixture]
    [DotMemoryUnit(FailIfRunWithoutSupport = false)]
    public class SlidingConcurrentCacheTests
    {
        #region ValueFactories
        private static Task<string> ValueFactoryForExpireTest(int arg)
        {
            Assert.Pass("Successfully landed mis-cache");
            return Task.FromResult(arg.ToString());
        }

        private static Task<string> ValueFactoryForGetOrAddTest(int i)
        {
            Assert.Fail("Cache hit didn't occur");
            return Task.FromResult(i.ToString());
        }

        private static Task<string> ValueFactoryForRenewCacheTest(int i) => Task.FromResult(i.ToString());

        private static Task<string> ValueFactoryForMemoryTest1(Guid guid) => Task.FromResult(Convert.ToBase64String(guid.ToByteArray(), Base64FormattingOptions.None));
        #endregion

        [Test, DotMemoryUnit(CollectAllocations = true)]
        public async Task MemoryTest1()
        {
           ISlidingConcurrentCache<Guid, string> tempCache;

            using (tempCache = new SlidingConcurrentCache<Guid, string>(1000))
            {
                await Task.Run(() => Parallel.For(0, 1000000, async i =>
                {
                    Guid guid = Guid.NewGuid();
                    await tempCache.GetOrAdd(guid, ValueFactoryForMemoryTest1, 1).ConfigureAwait(false);
                })).ConfigureAwait(false);

                await Task.Delay(1000).ConfigureAwait(false);
                Assert.AreEqual(0, tempCache.CachedItemCount);
            }

            // ReSharper disable once UnusedVariable
            Assert.ThrowsAsync<ObjectDisposedException>(async () => { bool temp = await Task.FromResult(tempCache.IsDisposed).ConfigureAwait(false); });
        }


        [Test]
        public void DisposeTest()
        {
            ISlidingConcurrentCache<int, string> tempCache;
            TestDelegate testDelegate;

            using (tempCache = new SlidingConcurrentCache<int, string>(100))
            {
                // ReSharper disable once UnusedVariable
                testDelegate = () => { bool disposed = tempCache.IsDisposed; };
                Assert.IsFalse(tempCache.IsDisposed);
                Assert.DoesNotThrow(testDelegate);
            }

            Assert.Throws<ObjectDisposedException>(testDelegate);
        }

        [Test]
        public async Task RenewCacheTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>(100);

            await tempCache.GetOrAdd(3, i => Task.FromResult(i.ToString()), 1).ConfigureAwait(false);
            await Task.Delay(1100).ConfigureAwait(false);
            string s = await tempCache.GetOrAdd(3, ValueFactoryForRenewCacheTest, 1).ConfigureAwait(false);

            Assert.AreEqual(3.ToString(), s);
        }

        [Test]
        public async Task SlideTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>(100);

            await tempCache.GetOrAdd(2, i => Task.FromResult(i.ToString()), 1, 1).ConfigureAwait(false);

            await Task.Delay(500).ConfigureAwait(false);
            await tempCache.GetOrAdd(2, ValueFactoryForGetOrAddTest).ConfigureAwait(false); //Must fetch from cache

            await Task.Delay(550).ConfigureAwait(false);
            await tempCache.GetOrAdd(2, ValueFactoryForGetOrAddTest).ConfigureAwait(false); //Must fetch from cache, even we passed 1 second mark. Cache should be slided.
            
            Assert.Pass();
        }

        [Test]
        public async Task ExpireTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>(100);

            await tempCache.GetOrAdd(1, i => Task.FromResult(i.ToString()), 1).ConfigureAwait(false);

            await Task.Delay(500).ConfigureAwait(false);
            await tempCache.GetOrAdd(1, ValueFactoryForGetOrAddTest).ConfigureAwait(false); //Must fetch from cache

            await Task.Delay(550).ConfigureAwait(false);
            await tempCache.GetOrAdd(1, ValueFactoryForExpireTest).ConfigureAwait(false); //Must fetch from repository
            Assert.Fail("Musn't hit here");
        }

        [Test]
        public async Task GetOrAddTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>();

            string addOne = await tempCache.GetOrAdd(1, i => Task.FromResult(i.ToString())).ConfigureAwait(false);
            Assert.IsFalse(string.IsNullOrEmpty(addOne));

            string actual = await tempCache.GetOrAdd(1, ValueFactoryForGetOrAddTest).ConfigureAwait(false);
            Assert.AreEqual(1.ToString(), actual);
        }

        [Test]
        public void SlidingConcurrentCacheTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>();
            Assert.IsNotNull(tempCache);
            Assert.False(tempCache.IsDisposed);
        }
    }
}