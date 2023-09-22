using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.RestApi.Services;

namespace SmartIOT.Connector.RestApi.Controllers.V1;

/// <summary>
/// This controller exposes methods to manage the connectors installed and running in a SmartIOT.Connector instance.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ConnectorController : ControllerBase
{
    private readonly SmartIotConnector _smartIotConnector;
    private readonly IConnectorService _connectorsService;

    /// <summary>
    /// ctor
    /// </summary>
    public ConnectorController(SmartIotConnector smartIotConnector, IConnectorService connectorsService)
    {
        _smartIotConnector = smartIotConnector;
        _connectorsService = connectorsService;
    }

    /// <summary>
    /// Returns the list of Connectors currently managed by SmartIOT.Connector
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Model.Connector>))]
    public IList<Model.Connector> GetConnectors()
    {
        return _smartIotConnector.Connectors.Select((x, i) => new Model.Connector(i, x.ConnectionString)).ToList();
    }

    /// <summary>
    /// Gets the zero based Connector as defined in the configuration or 404 not found
    /// </summary>
    /// <param name="index">The zero based index of the Connector</param>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [Route("{index}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Model.Connector))]
    public IActionResult GetConnector(int index)
    {
        var list = _smartIotConnector.Connectors;
        if (list.Count > index)
        {
            var c = list[index];
            return Ok(new Model.Connector(index, c.ConnectionString));
        }

        return NotFound();
    }

    /// <summary>
    /// Updates an existing Connector and completely replace it with the new one passed in the connectionString.
    /// </summary>
    /// <param name="index">The zero based index of the Connector</param>
    /// <param name="connectionString">The connectionString that defines the Connector</param>
    [HttpPut]
    [Route("{index}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Model.Connector))]
    public async Task<IActionResult> UpdateConnector(int index, [FromBody] string connectionString)
    {
        if (await _connectorsService.ReplaceConnectorAsync(index, connectionString))
            return Ok(new Model.Connector(index, connectionString));

        return NotFound();
    }

    /// <summary>
    /// Creates a new Connector as defined in the connectionString.
    /// </summary>
    /// <param name="connectionString">The connectionString that defines the Connector</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Model.Connector))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddConnector([FromBody] string connectionString)
    {
        var connector = await _connectorsService.AddConnectorAsync(connectionString);
        if (connector != null)
            return Ok(new Model.Connector(connector.Index, connector.ConnectionString));

        return BadRequest($"Invalid ConnectionString {connectionString}");
    }

    /// <summary>
    /// Deletes an already defined Connector. Be aware that deleting a Connector, may rescale other Connector indexes as well. Deleting the index 0, makes all Connectors scale their index by -1.
    /// </summary>
    /// <param name="index">The zero based index of the Connector</param>
    [HttpDelete]
    [Route("{index}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveConnector(int index)
    {
        if (await _smartIotConnector.RemoveConnectorAsync(index))
            return Ok();

        return NotFound();
    }
}
