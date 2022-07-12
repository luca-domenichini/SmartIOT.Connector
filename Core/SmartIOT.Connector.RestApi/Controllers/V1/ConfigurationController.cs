using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.RestApi.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartIOT.Connector.RestApi.Controllers.V1
{
	/// <summary>
	/// This controller exposes methods to manage the whole configuration of SmartIOT.Connector.
	/// </summary>
	[ApiController]
	[Route("api/v{version:apiVersion}/[controller]")]
	[ApiVersion("1.0")]
	public class ConfigurationController : ControllerBase
	{
		private readonly IConfigurationService _configurationService;

		public ConfigurationController(IConfigurationService configurationService)
		{
			_configurationService = configurationService;
		}

		/// <summary>
		/// This method returns the current configuration
		/// </summary>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SmartIotConnectorConfiguration))]
		public SmartIotConnectorConfiguration GetConfiguration()
		{
			return _configurationService.GetConfiguration();
		}

		/// <summary>
		/// This method persists the configuration on disk
		/// </summary>
		[HttpPut]
		[MapToApiVersion("1.0")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public IActionResult SaveConfiguration()
		{
			_configurationService.SaveConfiguration();

			return Ok();
		}
	}
}
