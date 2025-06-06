// This file is part of Hangfire. Copyright © 2019 Hangfire OÜ.
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
#if !NET451 && !NETSTANDARD1_3

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Annotations;
using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;
#if NET7_0
using Microsoft.Extensions.Hosting;
#else
using Microsoft.Extensions.Hosting;
#endif

namespace Hangfire
{
    public class BackgroundJobServerHostedService : IHostedService, IDisposable
    {
        private readonly BackgroundJobServerOptions _options;
        private readonly JobStorage _storage;
        private readonly IEnumerable<IBackgroundProcess> _additionalProcesses;
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
#endif
        private readonly IBackgroundJobFactory _factory;
        private readonly IBackgroundJobPerformer _performer;
        private readonly IBackgroundJobStateChanger _stateChanger;

        private IBackgroundProcessingServer _processingServer;

        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses)
#pragma warning disable 618
            : this(storage, options, additionalProcesses, null, null, null)
#pragma warning restore 618
        {
        }

#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IHostApplicationLifetime hostApplicationLifetime)
#pragma warning disable 618
            : this(storage, options, additionalProcesses, null, null, null, hostApplicationLifetime)
#pragma warning restore 618
        {
        }
#endif

#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        [Obsolete("This constructor uses an obsolete constructor overload of the BackgroundJobServer type that will be removed in 2.0.0.")]
        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger)
            : this(storage, options, additionalProcesses, factory, performer, stateChanger, null)
        {
        }
#endif

        [Obsolete("This constructor uses an obsolete constructor overload of the BackgroundJobServer type that will be removed in 2.0.0.")]
        public BackgroundJobServerHostedService(
            [NotNull] JobStorage storage,
            [NotNull] BackgroundJobServerOptions options,
            [NotNull] IEnumerable<IBackgroundProcess> additionalProcesses,
            [CanBeNull] IBackgroundJobFactory factory,
            [CanBeNull] IBackgroundJobPerformer performer,
            [CanBeNull] IBackgroundJobStateChanger stateChanger
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
            ,
            [CanBeNull] IHostApplicationLifetime hostApplicationLifetime
#endif
            )
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            _additionalProcesses = additionalProcesses;

            _factory = factory;
            _performer = performer;
            _stateChanger = stateChanger;

#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
            _hostApplicationLifetime = hostApplicationLifetime;
            _hostApplicationLifetime?.ApplicationStopping.Register(SendStopSignal);
#endif
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
            if (_hostApplicationLifetime != null)
            {
                // https://github.com/HangfireIO/Hangfire/issues/2117
                _hostApplicationLifetime.ApplicationStarted.Register(InitializeProcessingServer);
            }
            else
#endif
            {
                InitializeProcessingServer();
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var server = _processingServer;
            if (server == null) return;

            try
            {
                server.SendStop();
                await server.WaitForShutdownAsync(cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Due to a bug in ASP.NET Core's Testing package, the StopAsync method
                // can be called twice and from different threads, please see
                // https://github.com/dotnet/aspnetcore/issues/40271 for details.
                // This is not a case for regular applications but can fail integration tests
                // when the second call to the StopAsync method attempts to be performed on
                // an already disposed object.
                // This is not a big deal, however, it's still a workaround that should be
                // removed one day.
                // TODO: Remove this workaround and don't rely on this behavior for simplicity.
            }
        }

        public void Dispose()
        {
            _processingServer?.Dispose();
            _processingServer = null;
            GC.SuppressFinalize(this);
        }
        
        private void InitializeProcessingServer()
        {
            _processingServer = _factory != null && _performer != null && _stateChanger != null
#pragma warning disable 618
                ? new BackgroundJobServer(_options, _storage, _additionalProcesses, null, null, _factory, _performer,
                    _stateChanger)
#pragma warning restore 618
                : new BackgroundJobServer(_options, _storage, _additionalProcesses);
        }

#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        private void SendStopSignal()
        {
            try
            {
                _processingServer?.SendStop();
            }
            catch (ObjectDisposedException)
            {
                // Please see the comment regarding this exception above.
            }
        }
#endif
    }
}
#endif