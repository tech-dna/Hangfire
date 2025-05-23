﻿// This file is part of Hangfire. Copyright © 2021 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire
{
    internal sealed class DefaultClientManagerFactory : IBackgroundJobClientFactoryV2, IRecurringJobManagerFactoryV2
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultClientManagerFactory([NotNull] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IBackgroundJobClientV2 GetClientV2(JobStorage storage)
        {
            if (HangfireServiceCollectionExtensions.GetInternalServices(_serviceProvider, out var factory, out var stateChanger, out _))
            {
                return new BackgroundJobClient(storage, factory, stateChanger);
            }
            var scopeSetups = _serviceProvider.GetService<IEnumerable<IScopeSetup>>();
            foreach (var scopeSetup in scopeSetups)
            {

            }
            
            return new BackgroundJobClient(
                storage,
                _serviceProvider.GetRequiredService<IJobFilterProvider>());
        }

        public IBackgroundJobClient GetClient(JobStorage storage)
        {
            //var s = _serviceProvider.GetService<>
            return GetClientV2(storage);
        }

        public IRecurringJobManagerV2 GetManagerV2(JobStorage storage)
        {
            if (HangfireServiceCollectionExtensions.GetInternalServices(_serviceProvider, out var factory, out _, out _))
            {
                return new RecurringJobManager(
                    storage,
                    factory,
                    _serviceProvider.GetRequiredService<ITimeZoneResolver>());
            }

            return new RecurringJobManager(
                storage,
                _serviceProvider.GetRequiredService<IJobFilterProvider>(),
                _serviceProvider.GetRequiredService<ITimeZoneResolver>());
        }

        public IRecurringJobManager GetManager(JobStorage storage)
        {
            return GetManagerV2(storage);
        }
    }
}