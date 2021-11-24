using System;
using System.Threading.Tasks;

namespace LiveHome.Server.ClientInterfaces
{
    public interface IHomeServiceClient
    {
        Task ReceiveEnvironmentInfo(string user, string message);
        Task ReceiveCombustibleGasInfo(string user, bool message);
    }
}
