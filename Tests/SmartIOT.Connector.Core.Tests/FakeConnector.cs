using SmartIOT.Connector.Core.Events;
using System;
using System.Collections.Generic;

namespace SmartIOT.Connector.Core.Tests
{
	internal class FakeConnector : IConnector
	{
		private ISmartIOTConnectorInterface? _connectorInterface;

		public IList<TagScheduleEvent> TagReadEvents { get; } = new List<TagScheduleEvent>();
		public IList<TagScheduleEvent> TagWriteEvents { get; } = new List<TagScheduleEvent>();
		public IList<DeviceStatusEvent> DeviceStatusEvents { get; } = new List<DeviceStatusEvent>();
		public IList<ExceptionEventArgs> ExceptionEvents { get; } = new List<ExceptionEventArgs>();

		public void OnTagReadEvent(object? sender, TagScheduleEventArgs e)
		{
			TagReadEvents.Add(e.TagScheduleEvent);
		}

		public void OnTagWriteEvent(object? sender, TagScheduleEventArgs e)
		{
			TagWriteEvents.Add(e.TagScheduleEvent);
		}

		public void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs e)
		{
			DeviceStatusEvents.Add(e.DeviceStatusEvent);
		}

		public void OnException(object? sender, ExceptionEventArgs args)
		{
			ExceptionEvents.Add(args);
		}

		public void ClearEvents()
		{
			TagReadEvents.Clear();
			TagWriteEvents.Clear();
			DeviceStatusEvents.Clear();
			ExceptionEvents.Clear();
		}

		public void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			_connectorInterface = connectorInterface;
		}
		public void Stop()
		{

		}

		public void OnSchedulerStarting(object? sender, EventArgs e)
		{
			
		}

		public void OnSchedulerStopped(object? sender, EventArgs e)
		{
			
		}

		public void RequestTagWrite(string deviceId, string tagId, int startOffset, byte[] data)
		{
			_connectorInterface!.RequestTagWrite(deviceId, tagId, startOffset, data);
		}
	}
}
