using System;
using System.Threading.Tasks;

namespace Spectro.Core
{
    public interface IVisualizingOutput
    {
        TimeSpan? ActualLatency { get; }

        Task UpdateAsync(Analyzer analyzer);
    }
}
