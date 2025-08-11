using System;
using Hangfire.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.AspNetCore;

public class TestProvider : IServiceProvider
{
    private IServiceProvider _baseProvider;
    private readonly SerializedScopes _serializedScopes;
    public TestProvider(IServiceProvider baseProvider, SerializedScopes serializedScopes)
    {
        _baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
        _serializedScopes = serializedScopes;
    }
    public object GetService(Type serviceType)
    {
        var ser = serviceType.IsArray ? _serializedScopes.GetAll(serviceType) : _serializedScopes.GetServiceScope(serviceType);

        if (ser != null)
        {
            return ser;
        }

        //var s = ActivatorUtilities.GetServiceOrCreateInstance(this, serviceType);

        return serviceType == typeof(IServiceProvider) ?
            this
            : (_baseProvider.GetService(serviceType) ?? ActivatorUtilities.CreateInstance(this, serviceType));
    }
}
public class HangfireJobActivatorScope : JobActivatorScope
{
    private readonly IServiceScope _serviceScope;
    public IServiceProvider ServiceProvider => this._serviceScope.ServiceProvider;
    private readonly SerializedScopes _serializedScopes ;

    public HangfireJobActivatorScope([NotNull] IServiceScope serviceScope, SerializedScopes otherScopes)
    {
        this._serviceScope = serviceScope != null ? serviceScope : throw new ArgumentNullException(nameof(serviceScope));
        _serializedScopes = otherScopes?? new SerializedScopes();
    }

    public override object Resolve(Type type)
    {
        //var ser = type.IsArray ? _serializedScopes.GetAll(type) : _serializedScopes.GetServiceScope(type);

        //if (ser != null)
        //{
        //    return ser;
        //}

        //return ActivatorUtilities.GetServiceOrCreateInstance(new TestProvider(ServiceProvider, _serializedScopes), type);
        return new TestProvider(ServiceProvider, _serializedScopes).GetService(type);
    }

    public override void DisposeScope()
    {
        //var db = _serviceScope.ServiceProvider.GetService<DbContext>();
        //db.SaveChanges();
        //if (this._serviceScope is IAsyncDisposable serviceScope)
        //    serviceScope.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        //else
        this._serviceScope.Dispose();
    }
}