using System.Net;
using System.Net.Sockets;
using Ocultum.Web.Hosting;

namespace Ocultum.Tests.Unit;

public class PortFinderTests
{
    [Fact]
    public void Skips_ports_that_are_already_taken()
    {
        using var busy = new TcpListener(IPAddress.Loopback, 0);
        busy.Start();
        var taken = ((IPEndPoint)busy.LocalEndpoint).Port;

        var found = PortFinder.FindFree(taken, range: 20);

        found.Should().BeGreaterThan(taken);
    }

    [Fact]
    public void Gives_up_with_a_clear_message_when_the_range_is_exhausted()
    {
        using var busy = new TcpListener(IPAddress.Loopback, 0);
        busy.Start();
        var taken = ((IPEndPoint)busy.LocalEndpoint).Port;

        var act = () => PortFinder.FindFree(taken, range: 0);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Server:Port*");
    }
}
