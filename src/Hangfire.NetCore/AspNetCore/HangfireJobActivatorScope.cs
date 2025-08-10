using System;
using Hangfire.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.AspNetCore;

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
        var ser = type.IsArray ? _serializedScopes.GetAll(type) : _serializedScopes.GetServiceScope(type);

        if (ser != null)
        {
            return ser;
        }

        return type == typeof(IServiceProvider) ?
            _serviceScope.ServiceProvider
            : ActivatorUtilities.GetServiceOrCreateInstance(ServiceProvider, type);
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