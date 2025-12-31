using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.Common;
using Hangfire.Core.Tests.Mocks;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Hangfire.NetCore.Tests.AspNetCore
{
    interface ITestInterface
    {
        void SetValue(string value);
        string Value { get; }
    }

    class PropertyBag
    {
        public string Value { get; set; }
    }
    class TestInterface_Impl : ITestInterface
    {
        private PropertyBag _bag;
        public string Value => _bag.Value;
        public TestInterface_Impl(PropertyBag bag)
        {
            _bag = bag;
        }
        public void SetValue(string value)
        {
            _bag.Value = value;
        }
    }

    public class ScopedQueueClientTests
    {
        public class DummyJob
        {
            public void DoWork() { }
        }

        [Fact]
        public void Constructor_InitializesFields()
        {
            var jobStorage = new Mock<JobStorage>().Object;
            var jobFactory = new Mock<IBackgroundJobClientFactoryV2>().Object;
            var configurators = new List<IQueueScopeConfigurator>();

            var client = new ScopedQueueClient(jobStorage, jobFactory, configurators);

            Assert.NotNull(client);
        }

        [Fact]
        public void Enqueue_CallsClientCreate_WithExpectedParameters()
        {
            // Arrange
            var jobStorage = new Mock<JobStorage>().Object;
            var jobFactoryMock = new Mock<IBackgroundJobClientFactoryV2>();
            var clientV2Mock = new Mock<IBackgroundJobClientV2>();
            jobFactoryMock.Setup(f => f.GetClientV2(jobStorage)).Returns(clientV2Mock.Object);

            var configurators = new List<IQueueScopeConfigurator>();
            var client = new ScopedQueueClient(jobStorage, jobFactoryMock.Object, configurators);

            string expectedJobId = "job-123";
            clientV2Mock
                .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>(), It.IsAny<IDictionary<string, object>>()))
                .Returns(expectedJobId);

            Expression<Action<DummyJob>> expr = j => j.DoWork();

            // Act
            var jobId = client.Enqueue(expr);

            // Assert
            Assert.Equal(expectedJobId, jobId);
            clientV2Mock.Verify(c => c.Create(
                It.Is<Job>(j => j.Method.Name == nameof(DummyJob.DoWork)),
                It.Is<ScheduledState>(s => s != null),
                It.IsAny<IDictionary<string, object>>()), Times.Once);
        }

        [Fact]
        public void Enqueue_AddsScopeVar_WhenConfiguratorsPresent()
        {
            // Arrange
            var jobStorage = new Mock<JobStorage>().Object;
            var jobFactoryMock = new Mock<IBackgroundJobClientFactoryV2>();
            var clientV2Mock = new Mock<IBackgroundJobClientV2>();
            jobFactoryMock.Setup(f => f.GetClientV2(jobStorage)).Returns(clientV2Mock.Object);

            var scopes = new SerializedScopes(new[] { new SerializedScope("test") });
            var configuratorMock = new Mock<IQueueScopeConfigurator>();
            configuratorMock.Setup(c => c.SerializeScopes()).Returns(scopes);

            var configurators = new List<IQueueScopeConfigurator> { configuratorMock.Object };
            var client = new ScopedQueueClient(jobStorage, jobFactoryMock.Object, configurators);

            IDictionary<string, object> capturedParams = null;
            clientV2Mock
                .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>(), It.IsAny<IDictionary<string, object>>()))
                .Callback<Job, IState, IDictionary<string, object>>((job, state, param) => capturedParams = param)
                .Returns("job-456");

            Expression<Action<DummyJob>> expr = j => j.DoWork();

            // Act
            client.Enqueue(expr);

            // Assert
            Assert.NotNull(capturedParams);
            Assert.True(capturedParams.ContainsKey("___SCOPE_VAR"));
            Assert.IsType<SerializedScopes>(capturedParams["___SCOPE_VAR"]);
        }

        [Fact]
        public void Configure_WithJobActivatorScopeAndContext_ThrowsNotImplemented()
        {
            // Arrange
            var jobStorage = new Mock<JobStorage>().Object;
            var jobFactoryMock = new Mock<IBackgroundJobClientFactoryV2>();
            var configurators = new List<IQueueScopeConfigurator>();
            var client = new ScopedQueueClient(jobStorage, jobFactoryMock.Object, configurators);

            var scope = new Mock<JobActivatorScope>().Object;
            //var context = new Mock<JobActivatorContext>(null, null, null).Object;

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => client.Configure(scope, null));
        }

        [Fact]
        public void Configure_WithJobActivatorScopeAndInstance_ThrowsNotImplemented()
        {
            // Arrange
            var jobStorage = new Mock<JobStorage>().Object;
            var jobFactoryMock = new Mock<IBackgroundJobClientFactoryV2>();
            var configurators = new List<IQueueScopeConfigurator>();
            var client = new ScopedQueueClient(jobStorage, jobFactoryMock.Object, configurators);

            var scope = new Mock<JobActivatorScope>().Object;
            var instance = new object();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => client.Configure(scope, instance));
        }

        [Fact]
        public void TestServiceScopes()
        {
#if NET46_OR_GREATER
            var parentScopeCollection = new ServiceCollection();
            parentScopeCollection.AddScoped<PropertyBag>();
            parentScopeCollection.AddScoped<ITestInterface, TestInterface_Impl>();
            using (var parentProvider = parentScopeCollection.BuildServiceProvider())
            {
                var y = parentProvider.GetService<IServiceScopeFactory>();
                var activator = new HangfireJobActivator(y);
                var activatorContextMock = new JobActivatorContextMock();

                var serializedScopes = new SerializedScopes();
                serializedScopes.Add<ITestInterface>(new TestInterface_Impl(new PropertyBag { Value = "ok jim 33" }));
                using (var childActivatorScope = activator.BeginScope(activatorContextMock.Object, serializedScopes))
                {
                    var abc = childActivatorScope.Resolve(typeof(ITestInterface)) as ITestInterface;
                    var o = abc.Value;
                    Assert.Equal("ok jim 33", o);
                }

            }
#endif
        }
    }
}

