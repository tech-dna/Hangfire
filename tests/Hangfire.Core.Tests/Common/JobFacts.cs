using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Moq;
using Newtonsoft.Json;
using Xunit;

// ReSharper disable LocalizableElement
// ReSharper disable AssignNullToNotNullAttribute

#pragma warning disable 618

namespace Hangfire.Core.Tests.Common
{
    public class JobFacts
    {
        private static readonly DateTime SomeDateTime = new DateTime(2014, 5, 30, 12, 0, 0);
        private static bool _methodInvoked;
        private static bool _disposed;

        private readonly Type _type;
        private readonly MethodInfo _method;
        private readonly object[] _arguments;
        private readonly string _queue;
        private readonly Mock<JobActivator> _activator;
        private readonly Mock<IJobCancellationToken> _token;
        
        public JobFacts()
        {
            _type = typeof (JobFacts);
            _method = _type.GetMethod("StaticMethod");
            _arguments = new object[0];
            _queue = "critical";

            _activator = new Mock<JobActivator> { CallBase = true };
            _token = new Mock<IJobCancellationToken>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new Job(null, _method, _arguments));

            Assert.Equal("type", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheMethodIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new Job(_type, null, _arguments));

            Assert.Equal("method", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenTheTypeDoesNotContainTheGivenMethod()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Job(typeof(Job), _method, _arguments));

            Assert.Equal("type", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenArgumentsArrayIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                // ReSharper disable once AssignNullToNotNullAttribute
                () => new Job(_type, _method, (object[])null));

            Assert.Equal("args", exception.ParamName);
        }

        [Fact]
        public void Ctor_DoesNotThrow_WhenQueueIsNull()
        {
            var job = new Job(_type, _method, _arguments, null);
            Assert.NotNull(job);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenQueueValidationFails()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Job(_type, _method, _arguments, "&^*%"));

            Assert.Equal("queue", exception.ParamName);
        }

        [Fact]
        public void Ctor_InitializesAllProperties()
        {
            var job = new Job(_type, _method, _arguments);

            Assert.Same(_type, job.Type);
            Assert.Same(_method, job.Method);
            Assert.True(_arguments.SequenceEqual(job.Arguments));
            Assert.Null(job.Queue);
        }

        [Fact]
        public void Ctor_WithQueue_InitializesAllTheProperties()
        {
            var job = new Job(_type, _method, _arguments, _queue);

            Assert.Same(_type, job.Type);
            Assert.Same(_method, job.Method);
            Assert.True(_arguments.SequenceEqual(job.Args));
            Assert.Equal(_queue, job.Queue);
        }

        [Fact]
        public void Ctor_HasDefaultValueForArguments()
        {
            var job = new Job(_type, _method);

            Assert.Empty(job.Arguments);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenArgumentCountIsNotEqualToParameterCount()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new Job(_type, _method, new[] { "hello!" }));

            Assert.Contains("count", exception.Message);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContains_UnassignedGenericTypeParameters()
        {
            var method = _type.GetMethod("GenericMethod");

            Assert.Throws<NotSupportedException>(
                () => new Job(_type, method, new[] { "hello!" }));
        }

        [Fact]
        public void Ctor_CanUsePropertyValues_OfAnotherJob_AsItsArguments()
        {
            var method = _type.GetMethod("MethodWithArguments");
            var job = new Job(_type, method, "hello", 456);

            var anotherJob = new Job(job.Type, job.Method, job.Args);

            Assert.Equal(_type, anotherJob.Type);
            Assert.Equal(method, anotherJob.Method);
            Assert.Equal("hello", anotherJob.Args[0]);
            Assert.Equal(456, anotherJob.Args[1]);
        }

        [Fact]
        public void FromExpression_Action_ThrowsException_WhenNullExpressionProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression((Expression<Action>)null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromExpression_Action_DoesNotThrowAnException_WhenNullQueueProvided()
        {
            var job = Job.FromExpression(() => Console.WriteLine(), null);
            Assert.NotNull(job);
        }

        [Fact]
        public void FromExpression_ThrowsAnException_WhenNewExpressionIsGiven()
        {
            Assert.Throws<ArgumentException>(
                // ReSharper disable once ObjectCreationAsStatement
                () => Job.FromExpression(() => new JobFacts()));
        }

        [Fact]
        public void FromExpression_Action_ReturnsTheJob()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            Assert.Equal(typeof(Console), job.Type);
            Assert.Equal("WriteLine", job.Method.Name);
            Assert.Null(job.Queue);
        }

        [Fact]
        public void FromExpression_ActionWithQueue_ReturnsTheJobWithQueueSet()
        {
            var job = Job.FromExpression(() => Console.WriteLine(), "critical");

            Assert.Equal(typeof(Console), job.Type);
            Assert.Equal("WriteLine", job.Method.Name);
            Assert.Equal("critical", job.Queue);
        }

        [Fact]
        public void FromExpression_Func_ThrowsException_WhenNullExpressionProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression((Expression<Func<Task>>)null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromExpression_Func_DoesNotThrowAnException_WhenNullQueueProvided()
        {
            var job = Job.FromExpression(() => AsyncMethod(), null);
            Assert.NotNull(job);
        }

        [Fact]
        public void FromExpression_Func_ReturnsTheJob()
        {
            var job = Job.FromExpression(() => AsyncMethod());

            Assert.Equal(typeof(JobFacts), job.Type);
            Assert.Equal("AsyncMethod", job.Method.Name);
            Assert.Null(job.Queue);
        }

        [Fact]
        public void FromExpression_FuncWithQueue_ReturnsTheJobWithQueueSet()
        {
            var job = Job.FromExpression(() => AsyncMethod(), "critical");

            Assert.Equal(typeof(JobFacts), job.Type);
            Assert.Equal("AsyncMethod", job.Method.Name);
            Assert.Equal("critical", job.Queue);
        }

        [Fact]
        public void FromExpression_ConvertsDateTimeRepresentation_ToIso8601Format()
        {
            var date = new DateTime(2014, 5, 30, 12, 0, 0, 777);
            var expected = date.ToString("o");

            var job = Job.FromExpression(() => MethodWithDateTimeArgument(date));

            Assert.Equal(expected, job.Arguments[0]);
        }

        [Fact]
        public void FromExpression_ConvertsArgumentsToJson()
        {
            var job = Job.FromExpression(() => MethodWithArguments("123", 1));

            Assert.Equal("\"123\"", job.Arguments[0]);
            Assert.Equal("1", job.Arguments[1]);
        }

        [Fact]
        public void FromExpression_ConvertsObjectArgumentsToJson()
        {
            var job = Job.FromExpression(() => MethodWithObjectArgument("hello"));

            Assert.Equal("\"hello\"", job.Arguments[0]);
        }

        [Fact]
        public void FromExpression_ReturnValueDoesNotDepend_OnCurrentCulture()
        {
            var date = DateTime.UtcNow;

            CultureHelper.SetCurrentCulture("en-US");
            var enJob = Job.FromExpression(() => MethodWithDateTimeArgument(date));

            CultureHelper.SetCurrentCulture("ru-RU");
            var ruJob = Job.FromExpression(() => MethodWithDateTimeArgument(date));

            Assert.Equal(enJob.Arguments[0], ruJob.Arguments[0]);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodIsAsyncVoid()
        {
            var method = typeof(JobFacts).GetMethod(nameof(AsyncVoidMethod));

            Assert.Throws<NotSupportedException>(
                () => new Job(typeof(JobFacts), method, new string[0]));
        }

        [Fact]
        public void FromInstanceExpression_Action_ThrowsException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression((Expression<Action<JobFacts>>)null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_Action_DoesNotThrowAnException_WhenNullQueueIsProvided()
        {
            var job = Job.FromExpression<Instance>(x => x.Method(), null);
            Assert.NotNull(job);
        }

        [Fact]
        public void FromInstanceExpression_Func_ThrowsException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression((Expression<Func<JobFacts, Task>>)null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_Func_DoesNotThrowAnException_WhenNullQueueIsProvided()
        {
            var job = Job.FromExpression<Instance>(x => x.FunctionReturningTask(), null);
            Assert.NotNull(job);
        }

        [Fact]
        public void FromInstanceExpression_ThrowsAnException_WhenNewExpressionIsGiven()
        {
            Assert.Throws<ArgumentException>(
                // ReSharper disable once ObjectCreationAsStatement
                () => Job.FromExpression<JobFacts>(x => new JobFacts()));
        }

        [Fact]
        public void FromInstanceExpression_Action_ReturnsCorrectResult()
        {
            var job = Job.FromExpression<Instance>(x => x.Method());

            Assert.Equal(typeof(Instance), job.Type);
            Assert.Equal("Method", job.Method.Name);
            Assert.Null(job.Queue);
        }

        [Fact]
        public void FromInstanceExpression_ActionWithQueue_ReturnsCorrectResultWithQueueSet()
        {
            var job = Job.FromExpression<Instance>(x => x.Method(), "critical");

            Assert.Equal(typeof(Instance), job.Type);
            Assert.Equal("Method", job.Method.Name);
            Assert.Equal("critical", job.Queue);
        }

        [Fact]
        public void FromInstanceExpression_Func_ReturnsCorrectResult()
        {
            var job = Job.FromExpression<Instance>(x => x.FunctionReturningTask());

            Assert.Equal(typeof(Instance), job.Type);
            Assert.Equal("FunctionReturningTask", job.Method.Name);
            Assert.Null(job.Queue);
        }

        [Fact]
        public void FromInstanceExpression_FuncWithQueue_ReturnsCorrectResultWithQueueSet()
        {
            var job = Job.FromExpression<Instance>(x => x.FunctionReturningTask(), "critical");

            Assert.Equal(typeof(Instance), job.Type);
            Assert.Equal("FunctionReturningTask", job.Method.Name);
            Assert.Equal("critical", job.Queue);
        }

        [Fact]
        public void FromNonGenericExpression_InfersType_FromAGivenObject()
        {
            IDisposable instance = new Instance();
            var job = Job.FromExpression(() => instance.Dispose());

            Assert.Equal(typeof(Instance), job.Type);
        }

        [Fact]
        public void FromNonGenericExpression_InfersACorrectMethod_FromAGivenObject_WhenInterfaceTreeIsUsed()
        {
            IDisposable instance = new Instance();
            var job = Job.FromExpression(() => instance.Dispose());

            Assert.Equal(typeof(Instance), job.Method.DeclaringType);
        }

        [Fact]
        public void FromNonGenericExpression_ThrowsAnException_IfGivenObjectIsNull()
        {
            IDisposable instance = null;

            Assert.Throws<InvalidOperationException>(
                () => Job.FromExpression(() => instance.Dispose()));
        }

        [Fact]
        public void FromGenericExpression_InfersType_FromAGivenObject_AndHandlesAssignableParameters()
        {
            IServiceInterface<MyDerivedClass> service = new MyBaseClassService();
            MyDerivedClass myClass = new MyDerivedClass();

            var job = Job.FromExpression(() => service.MyMethod(myClass));

            Assert.Equal(typeof(MyBaseClassService), job.Type);
            Assert.Equal("MyMethod", job.Method.Name);
            Assert.Equal(typeof(MyBaseClassService), job.Method.DeclaringType);
        }

        [Fact]
        public void FromScopedExpression_HandlesGenericMethods()
        {
            CommandDispatcher dispatcher = new CommandDispatcher();
            var job = Job.FromExpression(() => dispatcher.DispatchTyped(123));

            Assert.Equal(typeof(CommandDispatcher), job.Type);
            Assert.Equal(typeof(CommandDispatcher), job.Method.DeclaringType);
        }

        [Fact]
        public void FromScopedExpression_HandlesMethodsDeclaredInBaseClasse()
        {
            DerivedInstance instance = new DerivedInstance();
            var job = Job.FromExpression(() => instance.Method());

            Assert.Equal(typeof(DerivedInstance), job.Type);
            Assert.Equal(typeof(Instance), job.Method.DeclaringType);
        }

        [Fact]
        public void FromScopedExpression_ThrowsWhenExplicitInterfaceImplementationIsPassed()
        {
            IService service = new ServiceImpl();
            Assert.Throws<NotSupportedException>(() => Job.FromExpression(() => service.Method()));
        }

        public interface IService
        {
            void Method();
        }

        public class ServiceImpl : IService
        {
            void IService.Method()
            {
            }
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContainsReferenceParameter()
        {
            string test = null;
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => MethodWithReferenceParameter(ref test)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContainsOutputParameter()
        {
            string test;
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => MethodWithOutputParameter(out test)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodIsNotPublic()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => PrivateMethod()));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodParametersContainADelegate()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => DelegateMethod(() => Console.WriteLine("Hey delegate!"))));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodParametersContainAnExpression()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => ExpressionMethod(() => Console.WriteLine("Hey expression!"))));
        }

        [Fact]
        public void Perform_ThrowsAnException_WhenActivatorIsNull()
        {
            var job = Job.FromExpression(() => StaticMethod());

            var exception = Assert.Throws<ArgumentNullException>(
                () => job.Perform(null, _token.Object));

            Assert.Equal("activator", exception.ParamName);
        }

        [Fact]
        public void Perform_ThrowsAnException_WhenCancellationTokenIsNull()
        {
            var job = Job.FromExpression(() => StaticMethod());

            var exception = Assert.Throws<ArgumentNullException>(
                () => job.Perform(_activator.Object, null));

            Assert.Equal("cancellationToken", exception.ParamName);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeStaticMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression(() => StaticMethod());

            job.Perform(_activator.Object, _token.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_CanInvokeInstanceMethods()
        {
            _methodInvoked = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform(_activator.Object, _token.Object);

            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_DisposesDisposableInstance_AfterPerformance()
        {
            _disposed = false;
            var job = Job.FromExpression<Instance>(x => x.Method());

            job.Perform(_activator.Object, _token.Object);

            Assert.True(_disposed);
        }

        [Fact, StaticLock]
        public void Perform_PassesArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithArguments("hello", 5));

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesObjectArguments_ToACallingMethod()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithObjectArgument("5"));

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see the `MethodWithArguments` method.
            Assert.True(_methodInvoked);
        }

#if !NETCOREAPP1_0
        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTime_IfItWasSerialized_UsingTypeConverter()
        {
            // Arrange
            _methodInvoked = false;
            var typeConverter = TypeDescriptor.GetConverter(typeof (DateTime));
            var convertedDate = typeConverter.ConvertToInvariantString(SomeDateTime);

            var type = typeof (JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            var job = new Job(type, method, new[] { convertedDate });

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }
#endif

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTime_IfItWasSerialized_UsingOldFormat()
        {
            // Arrange
            _methodInvoked = false;
            var convertedDate = SomeDateTime.ToString("MM/dd/yyyy HH:mm:ss.ffff");

            var type = typeof(JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            var job = new Job(type, method, new[] { convertedDate });

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_PassesCorrectDateTimeArguments()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => MethodWithDateTimeArgument(SomeDateTime));

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also the `MethodWithDateTimeArgument` method.
            Assert.True(_methodInvoked);
        }

        [Fact, StaticLock]
        public void Perform_WorksCorrectly_WithNullValues()
        {
            // Arrange
            _methodInvoked = false;
            var job = Job.FromExpression(() => NullArgumentMethod(null));

            // Act
            job.Perform(_activator.Object, _token.Object);

            // Assert - see also `NullArgumentMethod` method.
            Assert.True(_methodInvoked);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorThrowsAnException()
        {
            var exception = new InvalidOperationException();
            _activator.Setup(x => x.ActivateJob(It.IsAny<Type>())).Throws(exception);

            var job = Job.FromExpression(() => InstanceMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.Same(exception, thrownException.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenActivatorReturnsNull()
        {
            _activator.Setup(x => x.ActivateJob(It.IsNotNull<Type>())).Returns(null);
            var job = Job.FromExpression(() => InstanceMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
        }

        [Fact]
        public void Ctor_ThrowsJsonReaderException_OnArgumentsDeserializationFailure()
        {
            var type = typeof (JobFacts);
            var method = type.GetMethod("MethodWithDateTimeArgument");

            Assert.Throws<JsonReaderException>(
                () => new Job(type, method, new []{ JobHelper.ToJson("sdfa") }));
        }

        [Fact, StaticLock]
        public void Perform_ThrowsPerformanceException_OnDisposalFailure()
        {
            _methodInvoked = false;

            var job = Job.FromExpression<BrokenDispose>(x => x.Method());

            var exception = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.True(_methodInvoked);
            Assert.NotNull(exception.InnerException);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WithUnwrappedInnerException()
        {
            var job = Job.FromExpression(() => ExceptionMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.IsType<InvalidOperationException>(thrownException.InnerException);
            Assert.Equal("exception", thrownException.InnerException.Message);
        }

        [Fact]
        public void Perform_ThrowsPerformanceException_WhenAMethodThrowsTaskCanceledException()
        {
            var job = Job.FromExpression(() => TaskCanceledExceptionMethod());

            var thrownException = Assert.Throws<JobPerformanceException>(
                () => job.Perform(_activator.Object, _token.Object));

            Assert.IsType<TaskCanceledException>(thrownException.InnerException);
        }

        [Fact]
        public void Perform_RethrowsOperationCanceledException_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            var job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _token.Setup(x => x.ShutdownToken).Returns(new CancellationToken(true));
            _token.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() => job.Perform(_activator.Object, _token.Object));
        }

        [Fact]
        public void Run_RethrowsTaskCanceledException_WhenShutdownTokenIsCanceled()
        {
            // Arrange
            var job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _token.Setup(x => x.ShutdownToken).Returns(new CancellationToken(true));
            _token.Setup(x => x.ThrowIfCancellationRequested()).Throws<TaskCanceledException>();

            // Act & Assert
            Assert.Throws<TaskCanceledException>(() => job.Perform(_activator.Object, _token.Object));
        }

        [Fact]
        public void Run_RethrowsJobAbortedException()
        {
            // Arrange
            var job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _token.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);
            _token.Setup(x => x.ThrowIfCancellationRequested()).Throws<JobAbortedException>();

            // Act & Assert
            Assert.Throws<JobAbortedException>(() => job.Perform(_activator.Object, _token.Object));
        }

        [Fact]
        public void Run_ThrowsJobPerformanceException_InsteadOfOperationCanceled_WhenShutdownWasNOTInitiated()
        {
            // Arrange
            var job = Job.FromExpression(() => CancelableJob(JobCancellationToken.Null));
            _token.Setup(x => x.ShutdownToken).Returns(CancellationToken.None);
            _token.Setup(x => x.ThrowIfCancellationRequested()).Throws<OperationCanceledException>();

            // Act & Assert
            Assert.Throws<JobPerformanceException>(() => job.Perform(_activator.Object, _token.Object));
        }

        [Fact]
        public void Perform_ReturnsValue_WhenCallingFunctionReturningValue()
        {
            var job = Job.FromExpression<Instance>(x => x.FunctionReturningValue());

            var result = job.Perform(_activator.Object, _token.Object);

            Assert.Equal("Return value", result);
        }

        [Fact]
        public void GetTypeFilterAttributes_ReturnsCorrectAttributes()
        {
            var job = Job.FromExpression<Instance>(x => x.Method());
            var nonCachedAttributes = job.GetTypeFilterAttributes(false).ToArray();
            var cachedAttributes = job.GetTypeFilterAttributes(true).ToArray();

            Assert.Single(nonCachedAttributes);
            Assert.Single(cachedAttributes);

            Assert.True(nonCachedAttributes[0] is TestTypeAttribute);
            Assert.True(cachedAttributes[0] is TestTypeAttribute);
        }

        [Fact]
        public void GetMethodFilterAttributes_ReturnsCorrectAttributes()
        {
            var job = Job.FromExpression<Instance>(x => x.Method());
            var nonCachedAttributes = job.GetMethodFilterAttributes(false).ToArray();
            var cachedAttributes = job.GetMethodFilterAttributes(true).ToArray();

            Assert.Single(nonCachedAttributes);
            Assert.Single(cachedAttributes);

            Assert.True(nonCachedAttributes[0] is TestMethodAttribute);
            Assert.True(cachedAttributes[0] is TestMethodAttribute);
        }

        private static void PrivateMethod()
        {
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void MethodWithReferenceParameter(ref string a)
        {
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void MethodWithOutputParameter(out string a)
        {
            a = "hello";
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void StaticMethod()
        {
            _methodInvoked = true;
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        public void InstanceMethod()
        {
            _methodInvoked = true;
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void CancelableJob(IJobCancellationToken token)
        {
            token.ThrowIfCancellationRequested();
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void NullArgumentMethod(string[] argument)
        {
            _methodInvoked = true;
            Assert.Null(argument);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void MethodWithArguments(string stringArg, int intArg)
        {
            _methodInvoked = true;

            Assert.Equal("hello", stringArg);
            Assert.Equal(5, intArg);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void MethodWithObjectArgument(object argument)
        {
            _methodInvoked = true;

            Assert.Equal("5", argument);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        public void MethodWithCustomArgument(Instance argument)
        {
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        public void MethodWithDateTimeArgument(DateTime argument)
        {
            _methodInvoked = true;

            Assert.Equal(SomeDateTime, argument);
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void ExceptionMethod()
        {
            throw new InvalidOperationException("exception");
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public static void TaskCanceledExceptionMethod()
        {
            throw new TaskCanceledException();
        }

        [UsedImplicitly]
        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public void GenericMethod<T>(T arg)
        {
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        public Task AsyncMethod()
        {
            var source = new TaskCompletionSource<bool>();
            return source.Task;
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        public async void AsyncVoidMethod()
        {
            await Task.Yield();
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void DelegateMethod(Action action)
        {
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test")]
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
        public void ExpressionMethod(Expression<Action> expression)
        {
        }

        private interface ICommandDispatcher
        {
            void DispatchTyped<TCommand>(TCommand command);
        }

        private sealed class CommandDispatcher : ICommandDispatcher
        {
            public void DispatchTyped<TCommand>(TCommand command)
            {
            }
        }

        [TestType]
        public class Instance : IDisposable
        {
            [TestMethod]
            public void Method()
            {
                _methodInvoked = true;
            }

            public void Dispose()
            {
                _disposed = true;
            }

            public string FunctionReturningValue()
            {
                return "Return value";
            }

            public async Task FunctionReturningTask()
            {
                await Task.Yield();
            }

            public async Task FunctionReturningValueTask()
            {
                await Task.Yield();
            }

            public async Task<string> FunctionReturningTaskResultingInString(bool continueOnCapturedContext)
            {
                await Task.Yield();
                await Task.Delay(15).ConfigureAwait(continueOnCapturedContext);

                return FunctionReturningValue();
            }
            
            public ValueTask<string> FunctionReturningValueTaskResultingInString(bool continueOnCapturedContext)
            {
                return new ValueTask<string>(FunctionReturningTaskResultingInString(continueOnCapturedContext));
            }
        }

        public class DerivedInstance : Instance
        {
        }

        public class BrokenDispose : IDisposable
        {
            public void Method()
            {
                _methodInvoked = true;
            }

            public void Dispose()
            {
                throw new InvalidOperationException();
            }
        }

        // ReSharper disable once UnusedTypeParameter
        public class JobClassWrapper<T> : IDisposable where T : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public class TestTypeAttribute : JobFilterAttribute
        {
        }

        public class TestMethodAttribute : JobFilterAttribute
        {
        }

        class MyBaseClass
        {
            public string BaseProp { get; set; }
        }

        class MyDerivedClass : MyBaseClass
        {
            public int MyProperty { get; set; }
        }

        interface IServiceInterface<in T> where T : MyBaseClass
        {
            Task MyMethod(T input);
        }

        class MyBaseClassService : IServiceInterface<MyBaseClass>
        {
            public Task MyMethod(MyBaseClass input)
            {
                return Task.FromResult(true);
            }
        }
    }
}
