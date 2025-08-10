using Hangfire.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hangfire.NetCore.Tests.AspNetCore
{
    public class SerializedScopesTests
    {
        [Fact]
        public void DefaultConstructor_CreatesEmptyCollection()
        {
            var scopes = new SerializedScopes();
            Assert.Empty(scopes);
        }
        [Fact]
        public void Constructor_WithScopes_InitializesCollection()
        {
            var s1 = new SerializedScope("a");
            var s2 = new SerializedScope(123);
            var initial = new[] { s1, s2 };

            var scopes = new SerializedScopes(initial);

            Assert.Equal(2, scopes.Count());
            Assert.Contains(s1, scopes);
            Assert.Contains(s2, scopes);
        }

        [Fact]
        public void Merge_WithSerializedScopes_Merges()
        {
            var s1 = new SerializedScope("a");
            var s2 = new SerializedScope(123);
            var initial = new[] { s1, s2 };

            var r1 = new SerializedScope("a");
            var r2 = new SerializedScope(123);
            var initial2 = new[] { r1, r2 };

            var secondScopes = new SerializedScopes(initial2);
            var scopes = new SerializedScopes(initial)
                .Merge(secondScopes);

            Assert.Equal(4, scopes.Count());
            Assert.Contains(s1, scopes);
            Assert.Contains(r2, scopes);
        }

        [Fact]
        public void Add_AddsScopeToCollection()
        {
            var scopes = new SerializedScopes();
            var s = new SerializedScope("test");

            scopes.Add(s);

            Assert.Single(scopes);
            Assert.Contains(s, scopes);
        }

        [Fact]
        public void GetEnumerator_EnumeratesAllScopes()
        {
            var s1 = new SerializedScope("a");
            var s2 = new SerializedScope("b");
            var scopes = new SerializedScopes(new[] { s1, s2 });

            var list = new List<SerializedScope>();
            foreach (var scope in scopes)
            {
                list.Add(scope);
            }

            Assert.Equal(2, list.Count);
            Assert.Equal(s1, list[0]);
            Assert.Equal(s2, list[1]);
        }

        [Fact]
        public void IEnumerable_GetEnumerator_EnumeratesAllScopes()
        {
            var s1 = new SerializedScope("a");
            var s2 = new SerializedScope("b");
            IEnumerable<SerializedScope> scopes = new SerializedScopes(new[] { s1, s2 });

            var list = scopes.ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal(s1, list[0]);
            Assert.Equal(s2, list[1]);
        }

        [Fact]
        public void Add_NullScope_ThrowsArgumentNullException()
        {
            var scopes = new SerializedScopes();
            Assert.Throws<ArgumentNullException>(() => scopes.Add(null));
        }

        [Fact]
        public void Returns_Implementation_If_found()
        {
            var s1 = new SerializedScope("a");
            SerializedScopes scopes = new SerializedScopes(new[] { s1 });
            var result = scopes.GetServiceScope<string>();
            Assert.NotNull(result);
            Assert.Equal("a", result);
        }

        [Fact]
        public void Returns_Null_If_Implementation_Not_Found()
        {
            SerializedScopes scopes = new SerializedScopes();

            Assert.Null(scopes.GetServiceScope<string>());
        }

        [Fact]
        public void Returns_EnumerableImplementations_If_Multiple_found()
        {
            var s1 = new SerializedScope("a");
            var s2 = new SerializedScope("b");
            SerializedScopes scopes = new SerializedScopes(new[] { s1, s2 });
            var result = scopes.GetAll<string>();

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<string>>(result);
            //Assert.Equal(2, result.Count());
        }
        [Fact]
        public void Returns_NullEnumerableImplementations_If_None_found()
        {
            SerializedScopes scopes = new SerializedScopes();
            var result = scopes.GetAll<string>();

            Assert.Null(result);
            //Assert.Equal(2, result.Count());
        }

        [Fact]
        public void Array_Constructor_Adds()
        {
            var s1 = new SerializedScope("a");
            var s2 = new SerializedScope("b");
            SerializedScopes scopes = new SerializedScopes(new[] { s1, s2 });

            Assert.Equal(2, scopes.GetAll().Count());
        }
    }
}
