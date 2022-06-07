using Moq;
using System;
using System.Collections.Concurrent;

namespace SmartIOT.Connector.Core.Tests
{
	public class Mocks
	{
		private readonly ConcurrentDictionary<Type, Mock> _mocks = new ConcurrentDictionary<Type, Mock>();

		public Mock<T> Get<T>() where T : class
		{
			return (Mock<T>)_mocks.GetOrAdd(typeof(T), type => new Mock<T>());
		}

		public void Set<T>(Mock<T> mock) where T : class
		{
			_mocks[typeof(T)] = mock;
		}
		public T GetObject<T>() where T : class
		{
			return Get<T>().Object;
		}
	}
}
