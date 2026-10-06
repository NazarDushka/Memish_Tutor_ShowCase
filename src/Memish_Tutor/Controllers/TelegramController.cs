using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Memish_Tutor.Services;

namespace MemeEnglishBot.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly IUpdateHandlerService _handler;

    public TelegramController(IUpdateHandlerService handler)
    {
        _handler = handler;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        await _handler.HandleUpdateAsync(update);
        return Ok();
    }
}