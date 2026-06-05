namespace Insurance_Hub.Models
{
    public class Provider
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        /// <summary>Country of head office, e.g. "Kenya", "South Africa".</summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>Short company description shown on the detail page.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Provider aggregate rating out of 5.</summary>
        public double Rating { get; set; } = 4.0;

        // Navigation
        public ICollection<InsurancePlan> Plans { get; set; } = new List<InsurancePlan>();
    }
}
