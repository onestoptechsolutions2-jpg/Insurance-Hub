using System.Security.Claims;
using System.Text.Json;
using Insurance_Hub.Data;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Insurance_Hub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/chat")]
    public class AdminChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ChatAssistant _assistant;

        public AdminChatController(ApplicationDbContext db, ChatAssistant assistant)
        {
            _db = db;
            _assistant = assistant;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var messages = (await _db.ChatMessages
                .Where(m => m.UserId == UserId)
                .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                .Take(60).ToListAsync()).AsEnumerable().Reverse().ToList();
            ViewBag.Help = ChatAssistant.Help;
            return View("~/Views/Admin/Chat.cshtml", messages);
        }

        [HttpPost("send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string? text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return RedirectToAction(nameof(Index));
            if (text.Length > 500) text = text[..500];

            _db.ChatMessages.Add(new ChatMessage { UserId = UserId, Text = text });
            var reply = await _assistant.InterpretAsync(text);
            _db.ChatMessages.Add(new ChatMessage
            {
                UserId = UserId,
                FromApp = true,
                Text = reply.Text,
                OptionsJson = reply.Payload is null ? null : JsonSerializer.Serialize(reply.Payload),
                CreatedAt = DateTime.UtcNow.AddMilliseconds(1)
            });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("choose")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Choose(int id, int index)
        {
            var msg = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == id && m.UserId == UserId && m.FromApp && !m.Resolved);
            var payload = msg?.OptionsJson is null ? null : JsonSerializer.Deserialize<ChatPayload>(msg.OptionsJson);
            if (msg is null || payload is null || index < 0 || index >= payload.Choices.Count)
                return RedirectToAction(nameof(Index));

            // Claim the bubble first so a double tap can't record twice.
            var claimed = await _db.ChatMessages.Where(m => m.Id == id && !m.Resolved)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Resolved, true));
            if (claimed == 0) return RedirectToAction(nameof(Index));

            var choice = payload.Choices[index];
            string result;
            try
            {
                result = await _assistant.ExecuteAsync(choice);
            }
            catch (Exception e)
            {
                result = "Sorry, I couldn't do that: " + e.Message;
                await _db.ChatMessages.Where(m => m.Id == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.Resolved, false));
            }

            _db.ChatMessages.Add(new ChatMessage { UserId = UserId, Text = choice.Label.Split(" · ")[0] });
            _db.ChatMessages.Add(new ChatMessage { UserId = UserId, FromApp = true, Text = result, CreatedAt = DateTime.UtcNow.AddMilliseconds(1) });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            await _db.ChatMessages.Where(m => m.Id == id && m.UserId == UserId && m.FromApp)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Resolved, true));
            _db.ChatMessages.Add(new ChatMessage { UserId = UserId, FromApp = true, Text = "Okay, cancelled." });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
