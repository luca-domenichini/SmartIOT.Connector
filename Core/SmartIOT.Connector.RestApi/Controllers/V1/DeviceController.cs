using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.RestApi.Model;
using SmartIOT.Connector.RestApi.Services;

namespace SmartIOT.Connector.RestApi.Controllers.V1
{
	[ApiController]
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	public class DeviceController : ControllerBase
	{
		private readonly SmartIotConnector _smartIotConnector;
		private readonly IDeviceService _deviceService;

		public DeviceController(SmartIotConnector smartIotConnector, IDeviceService deviceService)
		{
			_smartIotConnector = smartIotConnector;
			_deviceService = deviceService;
		}

		#region Endpoint configuration/

		/// <summary>
		/// Returns the devices configuration
		/// </summary>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DeviceConfiguration>))]
		[Route("configuration")]
		public IActionResult GetConfiguration()
		{
			return Ok(_deviceService.GetDeviceConfigurations());
		}

		/// <summary>
		/// Returns the requested device configuration, or 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DeviceConfiguration))]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("configuration/{deviceId}")]
		public IActionResult GetDeviceConfiguration(string deviceId)
		{
			var device = _deviceService.GetDeviceConfiguration(deviceId);
			if (device != null)
				return Ok(device);

			return NotFound();
		}

		/// <summary>
		/// Adds a new device, or 400 if device already exists
		/// </summary>
		/// <param name="deviceConfiguration">The device configuration to add to the scheduler</param>
		[HttpPost]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Route("configuration")]
		public IActionResult AddDevice([FromBody] DeviceConfiguration deviceConfiguration)
		{
			try
			{
				_deviceService.AddDevice(deviceConfiguration);
				return Ok();
			}
			catch (ApplicationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Removes an existing device, or 400 if device already exists
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		[HttpDelete]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Route("configuration/{deviceId}")]
		public IActionResult RemoveDevice(string deviceId)
		{
			try
			{
				_deviceService.RemoveDevice(deviceId);
				return Ok();
			}
			catch (ApplicationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Returns true if the device is enabled, or 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("configuration/{deviceId}/enabled")]
		public IActionResult IsDeviceEnabled(string deviceId)
		{
			var device = _smartIotConnector.Schedulers
				.Select(x => x.Device)
				.FirstOrDefault(x => string.Equals(x.DeviceId, deviceId, StringComparison.InvariantCultureIgnoreCase));

			if (device == null)
				return NotFound();

			return Ok(device.IsEnabled());
		}

		/// <summary>
		/// Enables or disables a device. Returns 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="enabled">true or false to enable the device</param>
		[HttpPut]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("configuration/{deviceId}/enabled")]
		public IActionResult SetDeviceEnabled(string deviceId, [FromBody] bool enabled)
		{
			var device = _smartIotConnector.Schedulers
				.Select(x => x.Device)
				.FirstOrDefault(x => string.Equals(x.DeviceId, deviceId, StringComparison.InvariantCultureIgnoreCase));

			if (device == null)
				return NotFound();

			device.SetEnabled(enabled);

			return Ok();
		}

		/// <summary>
		/// Returns the requested tag configuration, or 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagId">The tagId to use</param>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TagConfiguration))]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("configuration/{deviceId}/tags/{tagId}")]
		public IActionResult GetTagConfiguration(string deviceId, string tagId)
		{
			var tag = _deviceService.GetTagConfiguration(deviceId, tagId);
			if (tag != null)
				return Ok(tag);

			return NotFound();
		}

		/// <summary>
		/// Adds a new tag, or 400 something wrong with the request
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagConfiguration">The Tag configuration to add</param>
		[HttpPost]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Route("configuration/{deviceId}/tags")]
		public IActionResult AddTag(string deviceId, [FromBody] TagConfiguration tagConfiguration)
		{
			try
			{
				_deviceService.AddTag(deviceId, tagConfiguration);
				return Ok();
			}
			catch (ApplicationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Removes an existing tag, or 400 something wrong with the request
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagId">The tagId to use</param>
		[HttpDelete]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Route("configuration/{deviceId}/tags/{tagId}")]
		public IActionResult RemoveTag(string deviceId, string tagId)
		{
			try
			{
				_deviceService.RemoveTag(deviceId, tagId);
				return Ok();
			}
			catch (ApplicationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Updates an existing tag, or 400 something wrong with the request
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagConfiguration">The updated configuration for the tag</param>
		[HttpPut]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Route("configuration/{deviceId}/tags")]
		public IActionResult UpdateTag(string deviceId, [FromBody] TagConfiguration tagConfiguration)
		{
			try
			{
				_deviceService.UpdateTag(deviceId, tagConfiguration);
				return Ok();
			}
			catch (ApplicationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		#endregion


		#region Endpoint data/

		/// <summary>
		/// Returns the requested data, or 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagId">The tagId to use</param>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TagData))]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("data/{deviceId}/{tagId}")]
		public IActionResult GetTagData(string deviceId, string tagId)
		{
			var data = _deviceService.GetTagData(deviceId, tagId);
			if (data != null)
				return Ok(data);

			return NotFound();
		}

		/// <summary>
		/// Updates the requested data, or 400 something wrong with the request
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagId">The tagId to use</param>
		/// <param name="tagData">The data to insert in the tag</param>
		[HttpPut]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[Route("data/{deviceId}/{tagId}")]
		public IActionResult SetTagData(string deviceId, string tagId, [FromBody] TagData tagData)
		{
			try
			{
				_deviceService.SetTagData(deviceId, tagId, tagData);
				return Ok();
			}
			catch (ApplicationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		#endregion


		#region Endpoint status/

		/// <summary>
		/// Returns the list of configured devices
		/// </summary>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Device>))]
		[Route("status")]
		public IActionResult GetDevices()
		{
			return Ok(_deviceService.GetDevices());
		}

		/// <summary>
		/// Returns the requested device, or 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Device))]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("status/{deviceId}")]
		public IActionResult GetDevice(string deviceId)
		{
			var device = _deviceService.GetDevice(deviceId);
			if (device != null)
				return Ok(device);

			return NotFound();
		}

		/// <summary>
		/// Returns the requested tag, or 404 if not found
		/// </summary>
		/// <param name="deviceId">The deviceId to use</param>
		/// <param name="tagId">The tagId to use</param>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Tag))]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[Route("status/{deviceId}/{tagId}")]
		public IActionResult GetTag(string deviceId, string tagId)
		{
			var tag = _deviceService.GetTag(deviceId, tagId);
			if (tag != null)
				return Ok(tag);

			return NotFound();
		}

		#endregion
	}
}
