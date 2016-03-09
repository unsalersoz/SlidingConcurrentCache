using System;
using System.Threading.Tasks;
using NUnit.Framework;
using SlidingConcurrentCache.Interface;
using SlidingConcurrentCache.Library;

namespace SlidingConcurrentCacheTests.Library
{
    [TestFixture]
    public class SlidingConcurrentCacheTests
    {
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

        [Test]
        public void DisposeTest()
        {
            ISlidingConcurrentCache<int, string> tempCache;
            TestDelegate testDelegate;

            using (tempCache = new SlidingConcurrentCache<int, string>(100))
            {
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

            await tempCache.GetOrAdd(3, i => Task.FromResult(i.ToString()), 1);
            await Task.Delay(1100);
            string s = await tempCache.GetOrAdd(3, ValueFactoryForRenewCacheTest, 1);

            Assert.AreEqual(3.ToString(), s);
        }

        [Test]
        public async Task SlideTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>(100);

            await tempCache.GetOrAdd(2, i => Task.FromResult(i.ToString()), 1, 1);

            await Task.Delay(500);
            await tempCache.GetOrAdd(2, ValueFactoryForGetOrAddTest); //Must fetch from cache

            await Task.Delay(550);
            await tempCache.GetOrAdd(2, ValueFactoryForGetOrAddTest); //Must fetch from cache, even we passed 1 second mark. Cache should be slided.
            
            Assert.Pass();
        }

        [Test]
        public async Task ExpireTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>(100);

            await tempCache.GetOrAdd(1, i => Task.FromResult(i.ToString()), 1);

            await Task.Delay(500);
            await tempCache.GetOrAdd(1, ValueFactoryForGetOrAddTest); //Must fetch from cache

            await Task.Delay(550);
            await tempCache.GetOrAdd(1, ValueFactoryForExpireTest); //Must fetch from repository
            Assert.Fail("Musn't hit here");
        }

        [Test]
        public async Task GetOrAddTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>();

            string addOne = await tempCache.GetOrAdd(1, i => Task.FromResult(i.ToString()));
            Assert.IsFalse(string.IsNullOrEmpty(addOne));

            string actual = await tempCache.GetOrAdd(1, ValueFactoryForGetOrAddTest);
            Assert.AreEqual(1.ToString(), actual);
        }

        [Test]
        public void SlidingConcurrentCacheTest()
        {
            ISlidingConcurrentCache<int, string> tempCache = new SlidingConcurrentCache<int, string>();
            Assert.IsNotNull(tempCache);
        }
    }
}