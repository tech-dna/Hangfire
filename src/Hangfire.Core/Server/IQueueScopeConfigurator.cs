using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Server
{ 
    public interface IQueueScopeConfigurator
    {
        void Configure(JobActivatorScope serviceProvider, JobActivatorContext context);
        void Configure(JobActivatorScope serviceProvider, object instance);
        //IServiceProvider Configure(CreatingContext context, object instance);
    }
}
