namespace Insurance_Hub.Models
{
    public enum PlanTier { Basic, Standard, Premium, Comprehensive }

    public class InsurancePlan
    {
        public int Id { get; set; }

        public string PlanName { get; set; } = string.Empty;

        /// <summary>Short marketing description shown on the card.</summary>
        public string Description { get; set; } = string.Empty;

        public decimal MonthlyPremium { get; set; }

        public InsuranceType Type { get; set; }

        public PlanTier Tier { get; set; } = PlanTier.Standard;

        /// <summary>Maximum payout / sum assured (0 = unlimited / contact provider).</summary>
        public decimal CoverageLimit { get; set; }

        /// <summary>Excess / deductible per claim.</summary>
        public decimal Deductible { get; set; }

        /// <summary>Pipe-separated list of key features, e.g. "Roadside Assistance|Windscreen Cover|Courtesy Car".</summary>
        public string Features { get; set; } = string.Empty;

        /// <summary>Provider star rating out of 5 (one decimal).</summary>
        public double Rating { get; set; } = 4.0;

        /// <summary>Highlighted as a popular/recommended plan on the UI.</summary>
        public bool IsPopular { get; set; }

        // Foreign key → Provider
        public int ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;

        public InsurancePlan() { }

        // Helper to get features as a list
        public IEnumerable<string> FeatureList =>
            string.IsNullOrWhiteSpace(Features)
                ? Enumerable.Empty<string>()
                : Features.Split('|', StringSplitOptions.RemoveEmptyEntries);
    }
}
