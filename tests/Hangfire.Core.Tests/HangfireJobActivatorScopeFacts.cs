using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Hangfire.NetCore.Tests.AspNetCore
{
    public class HangfireJobActivatorScopeTests
    {
        public class FakeClass: IFakeService{}
        public interface IFakeService { }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenServiceScopeIsNull()
        {
            // Arrange
            IServiceScope nullScope = null;
            var scopes = new SerializedScopes();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HangfireJobActivatorScope(nullScope, scopes));
        }

        [Fact]
        public void ServiceProvider_ReturnsServiceProviderFromScope()
        {
            // Arrange
            var providerMock = new Mock<IServiceProvider>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(s => s.ServiceProvider).Returns(providerMock.Object);

            var scopes = new SerializedScopes();
            var activatorScope = new HangfireJobActivatorScope(scopeMock.Object, scopes);

            // Act
            var result = activatorScope.ServiceProvider;

            // Assert
            Assert.Equal(providerMock.Object, result);
        }

        [Fact]
        public void Resolve_ReturnsFromSerializedScopes_GetAll_ForArrayType()
        {
            // Arrange
            var type = new FakeClass();
            var expected = new[] { new object() };

            var scopesMock = new SerializedScopes(new []{ new SerializedScope(type, new[] { typeof(IFakeService) })});

            var providerMock = new Mock<IServiceProvider>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(s => s.ServiceProvider).Returns(providerMock.Object);

            var activatorScope = new HangfireJobActivatorScope(scopeMock.Object, scopesMock);

            // Act
            var result = activatorScope.Resolve(typeof(IFakeService));

            // Assert
            Assert.Equal(type, result);
        }

        [Fact]
        public void Resolve_ReturnsFromSerializedScopes_Get_ForNonArrayType()
        {
            // Arrange
            var type = typeof(IFakeService);
            var expected = new object();

            var scopesMock = new Mock<SerializedScopes>();
            scopesMock.Setup(s => s.GetServiceScope(type)).Returns(expected);

            var providerMock = new Mock<IServiceProvider>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(s => s.ServiceProvider).Returns(providerMock.Object);

            var activatorScope = new HangfireJobActivatorScope(scopeMock.Object, scopesMock.Object);

            // Act
            var result = activatorScope.Resolve(type);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Resolve_ReturnsServiceProvider_WhenTypeIsIServiceProvider()
        {
            // Arrange
            var providerMock = new Mock<IServiceProvider>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(s => s.ServiceProvider).Returns(providerMock.Object);

            var scopes = new SerializedScopes();
            var activatorScope = new HangfireJobActivatorScope(scopeMock.Object, scopes);

            // Act
            var result = activatorScope.Resolve(typeof(IServiceProvider));

            // Assert
            Assert.Equal(providerMock.Object, result);
        }

        [Fact]
        public void Resolve_UsesActivatorUtilities_WhenNotFoundInSerializedScopes()
        {
            // Arrange
            var type = typeof(IFakeService);

            var scopesMock = new Mock<SerializedScopes>();
            scopesMock.Setup(s => s.GetServiceScope(type)).Returns((object)null);

            var providerMock = new Mock<IServiceProvider>();
            providerMock.Setup(p => p.GetService(type)).Returns(new FakeService());

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.SetupGet(s => s.ServiceProvider).Returns(providerMock.Object);

            var activatorScope = new HangfireJobActivatorScope(scopeMock.Object, scopesMock.Object);

            // Act
            var result = activatorScope.Resolve(type);

            // Assert
            Assert.IsAssignableFrom<IFakeService>(result);
        }

        [Fact]
        public void DisposeScope_DisposesUnderlyingScope()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            var scopes = new SerializedScopes();
            var activatorScope = new HangfireJobActivatorScope(scopeMock.Object, scopes);

            // Act
            activatorScope.DisposeScope();

            // Assert
            scopeMock.Verify(s => s.Dispose(), Times.Once);
        }

        private class FakeService : IFakeService { }
    }
}
