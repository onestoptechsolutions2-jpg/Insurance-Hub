using System.ComponentModel.DataAnnotations;

namespace Insurance_Hub.Models
{
    /// <summary>One bubble in an admin's private conversation with the app (see ChatAssistant).</summary>
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required, MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        public bool FromApp { get; set; }

        [Required, MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        /// <summary>JSON of the tap-to-confirm choices and links the app offered with this bubble.</summary>
        public string? OptionsJson { get; set; }

        public bool Resolved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
