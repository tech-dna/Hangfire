using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Client;
using Hangfire.Common;

namespace Hangfire.AspNetCore
{
    public static class ScopedQueueClientExtensions
    {
        public static CreatingContext AddScopeData(this CreatingContext context, SerializedScopes scopes)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (scopes == null) throw new ArgumentNullException(nameof(scopes));
            
            if (context.Parameters.TryGetValue("___SCOPE_VAR", out var scopeVar))
            {
                if (scopeVar is SerializedScopes existingScopes)
                {
                    existingScopes.Merge(scopes);
                    return context;
                }
            }
            context.Parameters["___SCOPE_VAR"] = scopes;

            return context;
        }
    }
}
