using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NimPulse.Api.Controllers.Api.V1;

/// <summary>
/// Ohne dies ist von außen nicht prüfbar, welcher Stand tatsächlich läuft, wenn man `:latest`
/// deployt statt eines gepinnten Tags — die Version kommt aus /VERSION, zur Build-Zeit in die
/// Assembly gebacken (siehe NimPulse.Api.csproj), nicht zur Laufzeit aus einer Datei gelesen.
/// </summary>
[ApiController]
[Route("api/v1/version")]
[AllowAnonymous]
public class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

        return Ok(new { version });
    }
}
