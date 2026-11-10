using System.Net;
using System.Net.Sockets;

namespace Ocultum.Web.Hosting;

public static class PortFinder
{
    /// <summary>Returns the first free loopback port starting at <paramref name="preferred"/>.</summary>
    public static int FindFree(int preferred, int range)
    {
        for (var port = preferred; port <= preferred + Math.Max(0, range) && port <= 65535; port++)
        {
            if (IsFree(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException(
            $"No free port was found between {preferred} and {preferred + range}. Change Server:Port in appsettings.json.");
    }

    private static bool IsFree(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
