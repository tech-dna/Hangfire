using System;
using System.Collections.Generic;
using Hangfire.AspNetCore;

namespace Hangfire
{ 
    public interface IQueueScopeConfigurator
    {
        void Configure(JobActivatorScope serviceProvider, JobActivatorContext context);
        void Configure(JobActivatorScope serviceProvider, object instance);
        //IServiceProvider Configure(CreatingContext context, object instance);
        SerializedScopes SerializeScopes();
    }
}
