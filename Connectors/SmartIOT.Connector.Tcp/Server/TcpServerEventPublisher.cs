using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartIOT.Connector.Tcp.Server
{
	public class TcpServerEventPublisher : IConnectorEventPublisher
	{


		public void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			throw new NotImplementedException();
		}

		public void PublishException(Exception exception)
		{
			throw new NotImplementedException();
		}

		public void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			throw new NotImplementedException();
		}

		public void Start(IConnector connector, ConnectorInterface connectorInterface)
		{
			throw new NotImplementedException();
		}

		public void Stop()
		{
			throw new NotImplementedException();
		}
	}
}
