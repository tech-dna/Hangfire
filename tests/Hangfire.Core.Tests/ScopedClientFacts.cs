using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.AspNetCore;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class SerializedScopeFacts
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var implementer = new List<string>();
            var interfaces = new[] { typeof(IEnumerable<string>), typeof(ICollection<string>) };

            // Act
            var scope = new SerializedScope(implementer, interfaces);

            // Assert
            Assert.Equal(implementer.GetType(), scope.Implementer);
            Assert.Contains(interfaces[0], scope.ImplementedInterfaces);
            Assert.Contains(interfaces[1], scope.ImplementedInterfaces);
            Assert.Equal(implementer.GetType().AssemblyQualifiedName, scope.ImplementerName);
        }
        [Fact]
        public void Constructor_AddsImplementerClass_As_ImplementedInterface()
        {
            // Arrange
            var implementer = new List<string>();
            var interfaces = new[] { typeof(IEnumerable<string>), typeof(ICollection<string>) };

            // Act
            var scope = new SerializedScope(implementer, interfaces);

            // Assert
            Assert.Equal(implementer.GetType(), scope.Implementer);
            Assert.Equal(implementer.GetType().AssemblyQualifiedName, scope.ImplementerName);
            Assert.Contains(implementer.GetType(), scope.ImplementedInterfaces);
        }

        [Fact]
        public void Implementer_Returns_Expected_Type_From_Deserialization()
        {
            var implementer = new Dictionary<int, string> { { 1, "test" } };
            // Arrange
            var interfaces = new[] { typeof(IDictionary<int, string>) };
            var scope = new SerializedScope(implementer, interfaces);
            var deserialized = scope.ToString();
            // Act

            var result = JsonConvert.DeserializeObject<SerializedScope>(deserialized);

            // Assert
            Assert.Equal(implementer.GetType(), result.Implementer);
        }

        [Fact]
        public void BackingObject_Returns_Expected_Object()
        {
            var implementer = new Dictionary<int, string> { { 1, "test" } };
            // Arrange
            var interfaces = new[] { typeof(IDictionary<int, string>) };
            var scope = new SerializedScope(implementer, interfaces);

            // Assert
            Assert.NotNull(scope.BackingObject<Dictionary<int, string>>());
        }
        [Fact]
        public void BackingObject_Returns_Expected_Object_From_Serialized()
        {
            var implementer = new Dictionary<int, string> { { 1, "test" } };
            // Arrange
            var interfaces = new[] { typeof(IDictionary<int, string>) };
            var scope = new SerializedScope(implementer, interfaces);

            var deserialized = scope.ToString();
            // Act

            var result = JsonConvert.DeserializeObject<SerializedScope>(deserialized)
                .BackingObject<Dictionary<int, string>>();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test", result[1]);
        }

        [Fact]
        public void Deserialization_Populates_Expected_Properties()
        {
            var implementer = new Dictionary<int, string> { { 1, "test" } };
            // Arrange
            var interfaces = new[] { typeof(IDictionary<int, string>) };
            var scope = new SerializedScope(implementer, interfaces);
            var deserialized = scope.ToString();
            // Act

            var result = JsonConvert.DeserializeObject<SerializedScope>(deserialized);


            // Assert
            Assert.NotNull(result.Implementer);
        }

        [Fact]
        public void ImplementedInterfaces_IsEmpty_When_No_Interfaces()
        {
            SerializedScope scope = new SerializedScope(new ClassNoInterface());

            Assert.NotNull(scope.ImplementedInterfaces);
        }
    }
    public class ClassNoInterface{}
}
