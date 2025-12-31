using System.Threading.Tasks;
using PNGTuber_GPTv2.Domain.Enums;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface ILogger
    {
        void Log(string message, LogLevel level);
        void Error(string message);
        void Warn(string message);
        void Info(string message);
        void Debug(string message);
    }
}
