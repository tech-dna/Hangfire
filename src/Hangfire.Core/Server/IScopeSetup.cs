using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    public interface IScopeSetup
    {
        void PreExecute(JobActivatorScope jobActivatorScope, PerformContext context, object instance);
        void Serialize(JobActivatorScope scope);
        void UnSerialize(JobActivatorScope scope);
    }
}
