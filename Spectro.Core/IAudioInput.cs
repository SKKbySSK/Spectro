using System.Threading.Tasks;

namespace Spectro.Core
{
    public interface IAudioInput : IAudioSource
    {
        event ValueEventHandler<float[]> Filled;

        Task InitializeAsync(AudioFormat format);

        Task<FormatResult> SupportsFormatAsync(AudioFormat format);

        Task StartAsync();

        Task StopAsync();
    }
}
