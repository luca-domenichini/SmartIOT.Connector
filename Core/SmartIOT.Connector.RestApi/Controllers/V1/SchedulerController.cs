using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartIOT.Connector.RestApi.Controllers.V1;

/// <summary>
/// This controller exposes methods to manage the scheduler configuration properties.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SchedulerController : ControllerBase
{
    private readonly SmartIotConnector _smartIotConnector;

    public SchedulerController(SmartIotConnector smartIotConnector)
    {
        _smartIotConnector = smartIotConnector;
    }

    /// <summary>
    /// This method returns the current configuration
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SchedulerConfiguration))]
    [SwaggerOperation("This method returns the current configuration")]
    public SchedulerConfiguration GetConfiguration()
    {
        return _smartIotConnector.SchedulerConfiguration;
    }

    /// <summary>
    /// This method changes the current scheduler configuration
    /// </summary>
    [HttpPut]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation("This method changes the current scheduler configuration")]
    public IActionResult SetConfiguration([FromBody] SchedulerConfiguration configuration)
    {
        configuration.CopyTo(_smartIotConnector.SchedulerConfiguration);

        return Ok();
    }
}
