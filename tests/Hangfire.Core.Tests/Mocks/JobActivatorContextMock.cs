using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Storage;
using Moq;

namespace Hangfire.Core.Tests.Mocks
{
    public class JobActivatorContextMock
    {
        private readonly Lazy<JobActivatorContext> _object;

        public JobActivatorContextMock()
        {
            _object = new Lazy<JobActivatorContext>(
                () => new JobActivatorContext(
                    Mock.Of<IStorageConnection>(),
                    new BackgroundJobMock().Object,
                    Mock.Of<IJobCancellationToken>())
                );
        }
        public JobActivatorContext Object => _object.Value;
    }
}
