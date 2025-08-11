using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire.AspNetCore
{
    public interface IQueueContext
    {
        T SetValue<T>(T newObject);
    }
    public class HangfireJobActivator : JobActivator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public HangfireJobActivator([NotNull] IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            var internalScope = _serviceScopeFactory.CreateScope();
            var parm = context.GetJobParameter<SerializedScopes>("___SCOPE_VAR");
            
            var scope = new HangfireJobActivatorScope(internalScope, parm);
            return scope;
        }
    }

    public enum SerializedScopeLifetime
    {
        Scoped,Singleton,Transient
    }

    public class ScopedQueueClient
    {
        private IEnumerable<IQueueScopeConfigurator> _configurators;
        private IBackgroundJobClientFactoryV2 _jobFactory;
        private JobStorage _jobStorage;
        public ScopedQueueClient(JobStorage jobStorage, IBackgroundJobClientFactoryV2 jobFactory, IEnumerable<IQueueScopeConfigurator> configurators)
        {
            _configurators = configurators;
            _jobFactory = jobFactory;
            _jobStorage = jobStorage;
        }
        public string Enqueue<T> (Expression<Action<T>> act, int delay = 5000, string queue = null)
        {
            var param = new Dictionary<string, object>() as IDictionary<string, object>;
            if (_configurators.Any())
            {
                var primaryScopes = new SerializedScopes();
                //parameterObjects.AddRange(_configurators.Select(configurator => configurator.Serialize()));
                foreach (var configurator in _configurators)
                {
                    var scopes = configurator.SerializeScopes();
                    if (scopes != null && scopes.Any())
                    {
                        primaryScopes.Merge(scopes);
                    }
                }
                if (primaryScopes.Any())
                {
                    param.Add("___SCOPE_VAR", primaryScopes);
                }
            }
            var client = _jobFactory.GetClientV2(_jobStorage);
            var job = Job.FromExpression(act);
            
            var state = new ScheduledState(TimeSpan.FromMilliseconds(delay));
            
            return client.Create(job, state, param);
        }
        public void Configure(JobActivatorScope serviceProvider, JobActivatorContext context)
        {
            throw new NotImplementedException();
        }

        public void Configure(JobActivatorScope serviceProvider, object instance)
        {
            throw new NotImplementedException();
        }
        /*
         
           public IServiceProvider Configure(IServiceProvider serviceProvider, JobActivatorContext context)
           {
               //foreach (var arg in context.BackgroundJob.Job.Args)
               //{
               //    if (arg is not QueueContext qContext) continue;
               //    var req = serviceProvider.GetService<ICurrentRequest>();
               //    if (req == null) continue;
               //    req.SetUser(new DomainEventUser(qContext.UserName, qContext.UserId));
               //    req.Url = qContext.Url;
               //}

               return serviceProvider;
           }

           public IServiceProvider Configure(IServiceProvider serviceProvider, object? instance)
           {
               return serviceProvider;
               //var req = serviceProvider.GetService<ICurrentRequest>();
               //if (req == null) return serviceProvider;
               //switch (instance)
               //{
               //    default:
               //        return serviceProvider;
               //    case IWithQueueContext contextWithProp:
               //    {
               //        req.SetUser(new DomainEventUser(contextWithProp.context.UserName, contextWithProp.context.UserId));
               //        req.Url = contextWithProp.context.Url;
               //        return serviceProvider;
               //    }
               //    case IWithContextVars context:
               //        req.SetUser(new DomainEventUser(context.UserName, context.UserId));
               //        req.Url = context.Url;
               //        return serviceProvider;
               //}
           }
         *
         */
    }
}
