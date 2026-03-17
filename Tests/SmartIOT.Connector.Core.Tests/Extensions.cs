using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using Xunit.Abstractions;

namespace SmartIOT.Connector.Core.Tests;

public static class Extensions
{
    [DoesNotReturn]
    public static void Rethrow(this Exception ex)
    {
        ExceptionDispatchInfo.Throw(ex);
        throw ex;
    }
    
    /// <summary>
    /// Finds a random available TCP port by starting a TcpListener on port 0, which tells the OS to assign an available port.
    /// The assigned port is then retrieved and the listener is stopped, freeing the port for use in tests.
    /// </summary>
    /// <returns></returns>
    public static int GetRandomAvailablePort()
    {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        
        return port;
    }
}
