﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCrunch.Framework;
using Rocks.Caching.Tests.Stubs;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ImplicitlyCapturedClosure
// ReSharper disable UnusedMember.Global

namespace Rocks.Caching.Tests.GetWithLock
{
    [TestClass, Serial, ExclusivelyUses ("GetWithLockExtensions.Locks")]
    public class GetTests
    {
        #region Public methods

        [TestInitialize]
        public void TestInitialize ()
        {
            GetWithLockExtensions.Locks.Clear ();
        }


        [TestCleanup]
        public void TestCleanup ()
        {
            GetWithLockExtensions.Locks.Clear ();
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void SingleThread_InvokesGetValueFunctionOnce ()
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();
            var exec_count = 0;


            // act
            var actual = cache.Get
                (cache_key,
                 () =>
                 {
                     exec_count++;
                     return new CachableResult<string> ("aaa", CachingParameters.FromMinutes (1));
                 });


            // assert
            actual.Should ().Be ("aaa");

            cache.Values
                 .Should ().HaveCount (1)
                 .And.ContainKey (cache_key)
                 .WhichValue.Value.Should ().Be ("aaa");

            exec_count.Should ().Be (1);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void TwoThreads_WithConcurency_InvokesGetValueFunctionOnce ()
        {
            MultiThreadWithConcurency_InvokesGetValueFunctionOnce_Test (2, sta: false);
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void FiveThreads_WithConcurency_InvokesGetValueFunctionOnce ()
        {
            MultiThreadWithConcurency_InvokesGetValueFunctionOnce_Test (5, sta: false);
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void TwoThreads_WithConcurency_GetResultThrows_BothThrows ()
        {
            MultiThreadWithConcurency_GetResultThrows_BothThrows_Test (2, sta: false);
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void FiveThreads_WithConcurency_GetResultThrows_BothThrows ()
        {
            MultiThreadWithConcurency_GetResultThrows_BothThrows_Test (5, sta: false);
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public Task TwoTasks_WithConcurency_InvokesGetValueFunctionOnce ()
        {
            return MultiTaskWithConcurency_InvokesGetValueFunctionOnce_Test (2, sta: false);
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public Task FiveTasks_WithConcurency_InvokesGetValueFunctionOnce ()
        {
            return MultiTaskWithConcurency_InvokesGetValueFunctionOnce_Test (5, sta: false);
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void ResultDataIsNull_CachesIt ()
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();
            var exec_count = 0;

            var create_result = new Func<CachableResult<string>>
                (() =>
                 {
                     exec_count++;
                     return new CachableResult<string> (null, CachingParameters.FromMinutes (1));
                 });


            // act
            var result = cache.Get (cache_key, create_result);
            var result2 = cache.Get (cache_key, create_result);


            // assert
            cache.Values.Should ().HaveCount (1);
            result.Should ().BeNull ();
            result2.Should ().BeNull ();
            exec_count.Should ().Be (1);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void ResultDataIsNullAndDependencyKeysIncludeResult_DoesNotCache ()
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();
            var exec_count = 0;

            var create_result = new Func<CachableResult<string>>
                (() =>
                 {
                     exec_count++;

                     return new CachableResult<string>
                         (null,
                          new CachingParameters (TimeSpan.FromMinutes (1), dependencyKeys: new[] { "dependency key" }),
                          dependencyKeysIncludeResult: true);
                 });


            // act
            var result = cache.Get (cache_key, create_result);
            var result2 = cache.Get (cache_key, create_result);


            // assert
            cache.Values.Should ().NotContainKey ("Test");
            result.Should ().BeNull ();
            result2.Should ().BeNull ();
            exec_count.Should ().Be (2);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void ResultIsNull_DoesNotCache ()
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();
            var exec_count = 0;

            var create_result = new Func<CachableResult<string>>
                (() =>
                 {
                     exec_count++;
                     return (CachableResult<string>) null;
                 });


            // act
            var result = cache.Get (cache_key, create_result);
            var result2 = cache.Get (cache_key, create_result);


            // assert
            cache.Values.Should ().BeEmpty ();
            result.Should ().BeNull ();
            result2.Should ().BeNull ();
            exec_count.Should ().Be (2);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void ResultIsNull_ValueTypeResult_DoesNotCache ()
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();
            var exec_count = 0;

            var create_result = new Func<CachableResult<int>>
                (() =>
                 {
                     exec_count++;
                     return (CachableResult<int>) null;
                 });


            // act
            var result = cache.Get (cache_key, create_result);
            var result2 = cache.Get (cache_key, create_result);


            // assert
            cache.Values.Should ().BeEmpty ();
            result.Should ().Be (0);
            result2.Should ().Be (0);
            exec_count.Should ().Be (2);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        [TestMethod, Microsoft.VisualStudio.TestTools.UnitTesting.Timeout (1000)]
        public void NestedCalls_ReturnsItem ()
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();
            var exec_count_a = 0;
            var exec_count_b = 0;


            // act
            var result = cache.Get
                (cache_key,
                 () =>
                 {
                     exec_count_a++;

                     var res = cache.Get
                         ("NestedCalls_ReturnsItem",
                          () =>
                          {
                              exec_count_b++;
                              return new CachableResult<string> ("bbb", CachingParameters.FromDays (1));
                          });

                     return new CachableResult<string> (res, CachingParameters.FromDays (1));
                 });


            // assert
            result.Should ().Be ("bbb");
            cache.Values.Should ().HaveCount (2);
            exec_count_a.Should ().Be (1);
            exec_count_b.Should ().Be (1);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }

        #endregion

        #region Private methods

        private static void MultiThreadWithConcurency_InvokesGetValueFunctionOnce_Test (int maxThreads, bool sta)
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();

            var exec_count = 0;
            var start_event = new ManualResetEvent (false);

            var results = new string[maxThreads];

            var threads = Enumerable.Range (0, maxThreads).Select
                (x =>
                 {
                     var thread = new Thread
                         (() =>
                          {
                              start_event.WaitOne ();
                              results[x] = cache.Get
                                  (cache_key,
                                   () =>
                                   {
                                       Thread.Sleep (100);

                                       Interlocked.Increment (ref exec_count);

                                       return new CachableResult<string> ("aaa", CachingParameters.FromMinutes (1));
                                   });
                          });

                     thread.IsBackground = true;

                     if (sta)
                         thread.SetApartmentState (ApartmentState.STA);

                     return thread;
                 })
                                    .ToList ();


            // act
            threads.ForEach (t => t.Start ());
            start_event.Set ();
            threads.ForEach (t => t.Join ());


            // assert
            cache.Values
                 .Should ().HaveCount (1)
                 .And.ContainKey (cache_key)
                 .WhichValue.Value.Should ().Be ("aaa");

            results.Should ().OnlyContain (x => x == "aaa");
            exec_count.Should ().Be (1);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        private static void MultiThreadWithConcurency_GetResultThrows_BothThrows_Test (int maxThreads, bool sta)
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();

            var exec_count = 0;
            var exception_count = 0;
            var start_event = new ManualResetEvent (false);

            var threads = Enumerable.Range (0, maxThreads).Select
                (x =>
                 {
                     var thread = new Thread
                         (() =>
                          {
                              start_event.WaitOne ();

                              try
                              {
                                  cache.Get<string>
                                      (cache_key,
                                       () =>
                                       {
                                           Thread.Sleep (100);

                                           Interlocked.Increment (ref exec_count);

                                           throw new InvalidOperationException ();
                                       });
                              }
                              catch (InvalidOperationException)
                              {
                                  Interlocked.Increment (ref exception_count);
                              }
                          });

                     thread.IsBackground = true;

                     if (sta)
                         thread.SetApartmentState (ApartmentState.STA);

                     return thread;
                 }).ToList ();


            // act
            threads.ForEach (t => t.Start ());
            start_event.Set ();
            threads.ForEach (t => t.Join ());


            // assert
            exec_count.Should ().Be (1);
            exception_count.Should ().Be (maxThreads);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }


        private static async Task MultiTaskWithConcurency_InvokesGetValueFunctionOnce_Test (int maxTasks, bool sta)
        {
            // arrange
            var cache = new CacheProviderStub ();
            var cache_key = Guid.NewGuid ().ToString ();

            var exec_count = 0;
            var start_event = new SemaphoreSlim (0, maxTasks);

            var results = new string[maxTasks];

            var task_factory = sta ? StaTaskScheduler.DefaultTaskFactory : Task.Factory;

            var tasks = Enumerable.Range (0, maxTasks).Select
                (x => task_factory.StartNew
                          (async () =>
                                 {
                                     await start_event.WaitAsync ();

                                     await Task.Delay (TimeSpan.FromMilliseconds (100));

                                     Interlocked.Increment (ref exec_count);

                                     var result = cache.Get
                                         (cache_key,
                                          () =>
                                          {
                                              Task.Delay (100).Wait ();

                                              return new CachableResult<string> ("aaa", CachingParameters.FromMinutes (10));
                                          });

                                     return result;
                                 }
                          ).Unwrap ()).ToList ();


            // act
            start_event.Release (maxTasks);
            await Task.WhenAll (tasks);


            // assert
            cache.Values
                 .Should ().HaveCount (1)
                 .And.ContainKey (cache_key)
                 .WhichValue.Value.Should ().Be ("aaa");

            results.Should ().OnlyContain (x => x == "aaa");
            exec_count.Should ().Be (1);

            GetWithLockExtensions.Locks.Should ().BeEmpty ();
        }

        #endregion
    }
}