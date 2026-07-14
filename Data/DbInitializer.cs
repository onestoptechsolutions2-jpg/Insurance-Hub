using System.Security.Cryptography;
using Insurance_Hub.Models;
using Insurance_Hub.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Insurance_Hub.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.MigrateAsync();

            if (await context.Providers.AnyAsync()) return;

            // ── PROVIDERS ────────────────────────────────────────────────────────
            var sanlam = new Provider
            {
                Name = "Sanlam",
                ContactEmail = "info@sanlam.co.za",
                Website = "https://www.sanlam.com",
                Country = "South Africa",
                Rating = 4.6,
                Description = "One of Africa's largest financial services groups, offering life, health, and general insurance across 33 countries. Founded in 1918, Sanlam is trusted by millions across the continent."
            };
            var britam = new Provider
            {
                Name = "Britam",
                ContactEmail = "info@ke.britam.com",
                Website = "https://ke.britam.com",
                Country = "Kenya",
                Rating = 4.5,
                Description = "A leading diversified financial services group operating across East and Central Africa, offering insurance, asset management and related financial products to over 1 million clients."
            };
            var oldMutual = new Provider
            {
                Name = "Old Mutual",
                ContactEmail = "clientservice@oldmutual.com",
                Website = "https://www.oldmutual.com",
                Country = "South Africa",
                Rating = 4.4,
                Description = "A pan-African financial services company with over 175 years of heritage, listed on the JSE. Old Mutual provides insurance, savings, and investment solutions across Africa."
            };
            var apa = new Provider
            {
                Name = "APA Insurance",
                ContactEmail = "info@apainsurance.org",
                Website = "https://www.apainsurance.org",
                Country = "Kenya",
                Rating = 4.3,
                Description = "A subsidiary of Apollo Investments Limited, APA Insurance is one of Kenya's top general and life insurers, known for innovative micro-insurance and agricultural products."
            };
            var cic = new Provider
            {
                Name = "CIC Insurance Group",
                ContactEmail = "info@cicinsurancegroup.com",
                Website = "https://ke.cicinsurancegroup.com",
                Country = "Kenya",
                Rating = 4.2,
                Description = "The leading cooperative-based insurance group in East Africa, CIC is the undisputed market leader in agricultural and micro-insurance, serving farmers and cooperatives across Kenya."
            };
            var jubilee = new Provider
            {
                Name = "Jubilee Insurance",
                ContactEmail = "info@jubileeinsurance.com",
                Website = "https://jubileeinsurance.com/ke",
                Country = "Kenya",
                Rating = 4.7,
                Description = "East Africa's leading composite insurer with an AA- credit rating, serving over 450,000 clients. Jubilee offers life, health, and asset insurance across Kenya, Uganda, Tanzania, and beyond."
            };
            var aar = new Provider
            {
                Name = "AAR Insurance",
                ContactEmail = "info@aar-insurance.com",
                Website = "https://aar-insurance.com",
                Country = "Kenya",
                Rating = 4.4,
                Description = "Kenya's foremost health insurance specialist with over 100,000 members. AAR is renowned for its flagship Unlimicare plan offering unlimited cover with no sub-limits on chronic conditions."
            };
            var iceaLion = new Provider
            {
                Name = "ICEA LION Group",
                ContactEmail = "info@icealion.co.ke",
                Website = "https://icealion.co.ke",
                Country = "Kenya",
                Rating = 4.3,
                Description = "Formed from the merger of ICEA and Lion of Kenya, ICEA LION is one of Nairobi's oldest and most trusted insurers offering life assurance, general insurance, pensions, and asset management."
            };

            var providers = new[] { sanlam, britam, oldMutual, apa, cic, jubilee, aar, iceaLion };
            await context.Providers.AddRangeAsync(providers);
            await context.SaveChangesAsync();

            // ── PLANS ─────────────────────────────────────────────────────────────
            var plans = new List<InsurancePlan>
            {
                // ═══════════════════════════════
                // LIFE INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "Sanlam Matrix Life Cover",
                    Description    = "Comprehensive life cover with a built-in Wealth Bonus that accrues every 5 years until retirement. Includes cancer benefit and immediate payout within 2 days.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Premium,
                    MonthlyPremium = 4_500.00m,
                    CoverageLimit  = 5_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.7,
                    IsPopular      = true,
                    Features       = "Wealth Bonus every 5 years|Cancer benefit|Disability lump-sum payout|Income protection|Funeral expense cover|Payout within 2 days of claim",
                    ProviderId     = sanlam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Sanlam Income Protector",
                    Description    = "Replaces your income if you are temporarily or permanently disabled, ensuring your family's lifestyle is maintained.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 2_800.00m,
                    CoverageLimit  = 0m,
                    Deductible     = 0m,
                    Rating         = 4.5,
                    IsPopular      = false,
                    Features       = "Temporary disability cover|Permanent disability cover|Monthly income replacement|Premium waiver on disability|Dread disease add-on",
                    ProviderId     = sanlam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Whole Life Plan",
                    Description    = "Lifetime protection with a cash value savings component. Covers you for life and builds a legacy for your family.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 8_000.00m,
                    CoverageLimit  = 10_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.6,
                    IsPopular      = true,
                    Features       = "Lifetime coverage|Cash value accumulation|Policy loan facility|Critical illness rider|Last expense benefit|Flexible premium options",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Dhamana Savings Plan",
                    Description    = "A life insurance savings plan that grows your money while protecting you. Ideal for long-term wealth accumulation.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 3_000.00m,
                    CoverageLimit  = 2_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.4,
                    IsPopular      = false,
                    Features       = "Insurance + savings combo|Guaranteed maturity benefit|Life cover throughout term|Loyalty bonus at maturity|Flexible premium payment",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Old Mutual Whole Life Assurance",
                    Description    = "Provides lifelong protection with universal life and indexed universal life options. Serves both savings and protection needs.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Premium,
                    MonthlyPremium = 6_500.00m,
                    CoverageLimit  = 8_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.5,
                    IsPopular      = false,
                    Features       = "Universal life option|Indexed universal life|Permanent disability benefit|Physical impairment payout|Estate planning tool",
                    ProviderId     = oldMutual.Id
                },
                new InsurancePlan
                {
                    PlanName       = "ICEA LION Value Added Term (VATA)",
                    Description    = "Term assurance with extra value — integrates critical illness, disability coverage, and premium waivers in one plan.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 3_200.00m,
                    CoverageLimit  = 3_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Critical illness protection|Disability coverage|Premium waiver on disability|Convertible to whole life|Renewable at end of term",
                    ProviderId     = iceaLion.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Pumzisha Retirement Plan",
                    Description    = "A life and pension product designed to ensure a comfortable retirement, with life cover throughout the savings period.",
                    Type           = InsuranceType.Life,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 4_000.00m,
                    CoverageLimit  = 4_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Retirement savings + life cover|Tax-deductible premiums|Flexible retirement age|Annuity or lump-sum payout|Death benefit to beneficiaries",
                    ProviderId     = apa.Id
                },

                // ═══════════════════════════════
                // HEALTH INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "AAR Unlimicare",
                    Description    = "Kenya's most comprehensive health plan with NO limits, NO sub-limits, and NO exclusions on pre-existing or chronic conditions. Truly unlimited cover.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 12_000.00m,
                    CoverageLimit  = 0m,
                    Deductible     = 5_000m,
                    Rating         = 4.9,
                    IsPopular      = true,
                    Features       = "Unlimited inpatient cover|Unlimited outpatient|Pre-existing conditions covered|Chronic conditions covered|Telemedicine included|Dental & optical|Maternity cover|Wellness programme",
                    ProviderId     = aar.Id
                },
                new InsurancePlan
                {
                    PlanName       = "AAR ShwAARi Medical Cover",
                    Description    = "Tailored health plans for all ages from 0–100. Flexible payment starting from Ksh 1,600/month with comprehensive inpatient and outpatient benefits.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 1_800.00m,
                    CoverageLimit  = 1_000_000m,
                    Deductible     = 2_000m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Covers age 0–100|Inpatient hospitalization|Outpatient consultations|Emergency evacuation|Last expense cover|Flexible payment frequency",
                    ProviderId     = aar.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Milele Health Cover",
                    Description    = "Individual and family health cover with 6 levels of coverage, access to a nationwide hospital network, and built-in wellness benefits.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Premium,
                    MonthlyPremium = 6_500.00m,
                    CoverageLimit  = 5_000_000m,
                    Deductible     = 3_000m,
                    Rating         = 4.6,
                    IsPopular      = true,
                    Features       = "6 coverage levels|Nationwide hospital network|Wellness & preventive care|Inpatient & outpatient|Dental & optical add-on|Maternity benefits|Last expense cover",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Kinga Ya Mkulima",
                    Description    = "Wholesome family medical and life cover for farming families — covers inpatient, outpatient, and last expense benefit at an accessible price.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 1_100.00m,
                    CoverageLimit  = 500_000m,
                    Deductible     = 1_000m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Inpatient cover|Outpatient cover|Last expense benefit|Family cover (up to 6)|Low monthly premium|Agricultural community focused",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Jubilee J-Care Health Plan",
                    Description    = "Flexible health insurance with 6 tiers of coverage. Jubilee's Family Physician Programme provides personalised, wellness-focused healthcare for members.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Premium,
                    MonthlyPremium = 7_500.00m,
                    CoverageLimit  = 6_000_000m,
                    Deductible     = 4_000m,
                    Rating         = 4.7,
                    IsPopular      = true,
                    Features       = "6 flexible coverage tiers|Family Physician Programme|Wellness & preventive focus|Regional hospital access|Maternity cover|Dental & optical|Medical evacuation",
                    ProviderId     = jubilee.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Old Mutual SME Health Cover",
                    Description    = "Designed for small and medium enterprises — covers inpatient, outpatient, maternity, dental, and optical for employees and their dependents.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 5_500.00m,
                    CoverageLimit  = 3_000_000m,
                    Deductible     = 2_500m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Inpatient & outpatient|Maternity cover|Dental & optical|WIBA compliance|Minimum 3 employees|Group rates available",
                    ProviderId     = oldMutual.Id
                },
                new InsurancePlan
                {
                    PlanName       = "CIC Co-opCare Health Plan",
                    Description    = "Specially designed health cover for cooperative members and their families. Affordable group rates with comprehensive benefits.",
                    Type           = InsuranceType.Health,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 3_800.00m,
                    CoverageLimit  = 2_000_000m,
                    Deductible     = 1_500m,
                    Rating         = 4.1,
                    IsPopular      = false,
                    Features       = "Co-operative member discount|Inpatient hospitalization|Outpatient consultations|Last expense cover|Dental cover|Optical cover",
                    ProviderId     = cic.Id
                },

                // ═══════════════════════════════
                // MOTOR INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "Jubilee Comprehensive Motor",
                    Description    = "Kenya's most complete motor insurance — covers accidental damage, theft, fire, and third-party liabilities with windscreen and roadside assistance included.",
                    Type           = InsuranceType.Motor,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 8_500.00m,
                    CoverageLimit  = 2_000_000m,
                    Deductible     = 15_000m,
                    Rating         = 4.7,
                    IsPopular      = true,
                    Features       = "Accidental damage cover|Theft & fire protection|Third-party liability|Windscreen cover|Roadside assistance 24/7|Courtesy car|Passenger liability|Flood & riots cover",
                    ProviderId     = jubilee.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Comprehensive Motor",
                    Description    = "Broadest vehicle protection covering accidental damage, theft, fire, and third-party liabilities. Includes COMESA yellow card for regional travel.",
                    Type           = InsuranceType.Motor,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 8_000.00m,
                    CoverageLimit  = 1_500_000m,
                    Deductible     = 15_000m,
                    Rating         = 4.5,
                    IsPopular      = false,
                    Features       = "Accidental damage|Theft & fire|Third-party liability|COMESA yellow card|Windscreen cover|Roadside assistance|Political violence cover",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Third Party, Fire & Theft",
                    Description    = "Mid-level motor cover protecting against fire, theft, and third-party liabilities — more than the minimum, below comprehensive pricing.",
                    Type           = InsuranceType.Motor,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 2_200.00m,
                    CoverageLimit  = 500_000m,
                    Deductible     = 10_000m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Fire protection|Theft cover|Third-party bodily injury|Third-party property damage|COMESA yellow card|Legal liability cover",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Motor Private Comprehensive",
                    Description    = "Full comprehensive cover for private vehicles with extended benefits including passenger liability and accessories cover.",
                    Type           = InsuranceType.Motor,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 7_800.00m,
                    CoverageLimit  = 1_800_000m,
                    Deductible     = 12_000m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Accidental damage|Theft & fire|Passenger liability|Accessories cover|Emergency medical expenses|Towing & recovery|Windscreen replacement",
                    ProviderId     = apa.Id
                },
                new InsurancePlan
                {
                    PlanName       = "ICEA LION Motor Comprehensive",
                    Description    = "Covers damage to car, accessories, and spare parts from accidents, fire, floods, and riots, plus full third-party liabilities.",
                    Type           = InsuranceType.Motor,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 7_500.00m,
                    CoverageLimit  = 1_600_000m,
                    Deductible     = 13_000m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Accidental damage|Fire & floods|Riots & civil commotion|Third-party liability|Accessories & spare parts|Free windscreen cover",
                    ProviderId     = iceaLion.Id
                },
                new InsurancePlan
                {
                    PlanName       = "CIC Third Party Motor",
                    Description    = "Minimum legal motor cover in Kenya at an affordable cooperative rate — ideal for low-mileage or older vehicles.",
                    Type           = InsuranceType.Motor,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 700.00m,
                    CoverageLimit  = 200_000m,
                    Deductible     = 5_000m,
                    Rating         = 4.0,
                    IsPopular      = false,
                    Features       = "Meets minimum legal requirement|Third-party bodily injury|Third-party property damage|Cooperative member discount|Quick claims processing",
                    ProviderId     = cic.Id
                },

                // ═══════════════════════════════
                // HOME INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "ICEA LION HomeBora",
                    Description    = "All-in-one home insurance bundling building, contents, jewelry, mobile phones, domestic employee cover, and personal liability under one policy.",
                    Type           = InsuranceType.Home,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 1_200.00m,
                    CoverageLimit  = 3_000_000m,
                    Deductible     = 10_000m,
                    Rating         = 4.4,
                    IsPopular      = true,
                    Features       = "Building structure cover|Home contents|Jewelry & valuables|Mobile phones & electronics|Domestic employee cover|Personal liability|Burglary & theft|Fire & allied perils",
                    ProviderId     = iceaLion.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Old Mutual Home Comprehensive",
                    Description    = "Comprehensive home package compensating for loss or damage to your building and its contents — from structure to the smallest personal item.",
                    Type           = InsuranceType.Home,
                    Tier           = PlanTier.Premium,
                    MonthlyPremium = 1_500.00m,
                    CoverageLimit  = 4_000_000m,
                    Deductible     = 10_000m,
                    Rating         = 4.4,
                    IsPopular      = false,
                    Features       = "Building structure|Home contents|Loss of rent|Personal liability|Accidental damage|Theft & burglary|Natural disasters|Temporary accommodation",
                    ProviderId     = oldMutual.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Jubilee Home Insurance",
                    Description    = "Protects your home and its contents against fire, theft, natural disasters, and personal liability with optional landlord cover.",
                    Type           = InsuranceType.Home,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 950.00m,
                    CoverageLimit  = 2_500_000m,
                    Deductible     = 8_000m,
                    Rating         = 4.5,
                    IsPopular      = false,
                    Features       = "Fire & allied perils|Theft & burglary|Natural disasters|Personal liability|Landlord option available|Home contents|Structural damage",
                    ProviderId     = jubilee.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Domestic Package",
                    Description    = "A convenient all-in-one domestic package covering the home building, contents, personal effects, and domestic workers.",
                    Type           = InsuranceType.Home,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 800.00m,
                    CoverageLimit  = 2_000_000m,
                    Deductible     = 8_000m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Building cover|Home contents|Personal effects|Domestic worker cover|All risk cover|Burglary protection|Fire protection",
                    ProviderId     = apa.Id
                },

                // ═══════════════════════════════
                // TRAVEL INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "Jubilee Global Travel Cover",
                    Description    = "Comprehensive international travel cover including medical expenses, trip cancellation, and special coverage for seniors aged 70–79.",
                    Type           = InsuranceType.Travel,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 2_500.00m,
                    CoverageLimit  = 5_000_000m,
                    Deductible     = 2_000m,
                    Rating         = 4.7,
                    IsPopular      = true,
                    Features       = "Emergency medical expenses|Trip cancellation|Flight delays & cancellations|Lost/stolen luggage|Personal liability|Hijacking cover|Senior travel (70-79)|Medical evacuation",
                    ProviderId     = jubilee.Id
                },
                new InsurancePlan
                {
                    PlanName       = "AAR Travel Insurance",
                    Description    = "End-to-end travel protection covering medical emergencies, lost property, trip cancellations, and personal liability worldwide.",
                    Type           = InsuranceType.Travel,
                    Tier           = PlanTier.Premium,
                    MonthlyPremium = 2_000.00m,
                    CoverageLimit  = 3_000_000m,
                    Deductible     = 1_500m,
                    Rating         = 4.4,
                    IsPopular      = false,
                    Features       = "Emergency medical|Personal accident|Lost documents & money|Luggage protection|Trip cancellation|Hijacking & detention|Personal liability|24/7 assistance",
                    ProviderId     = aar.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Travel Insurance",
                    Description    = "Affordable travel cover for individuals and families, including medical expenses, repatriation, and luggage protection.",
                    Type           = InsuranceType.Travel,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 1_500.00m,
                    CoverageLimit  = 2_000_000m,
                    Deductible     = 1_000m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Medical & hospitalisation|Medical repatriation|Lost luggage|Trip cancellation|Personal accident|Travel delay compensation",
                    ProviderId     = apa.Id
                },

                // ═══════════════════════════════
                // BUSINESS INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "ICEA LION BizBora Business Pack",
                    Description    = "A comprehensive non-motor business insurance bundle covering all key business risks in a single, easy-to-manage policy.",
                    Type           = InsuranceType.Business,
                    Tier           = PlanTier.Comprehensive,
                    MonthlyPremium = 12_000.00m,
                    CoverageLimit  = 10_000_000m,
                    Deductible     = 20_000m,
                    Rating         = 4.4,
                    IsPopular      = true,
                    Features       = "Fire & property damage|Burglary & theft|Public liability|Employer's liability|Business interruption|Money cover|Electronic equipment|Fidelity guarantee",
                    ProviderId     = iceaLion.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Britam Business Property & Fire",
                    Description    = "Protects business premises and stock against fire, theft, and natural disasters. Suitable for SMEs to large corporates.",
                    Type           = InsuranceType.Business,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 9_000.00m,
                    CoverageLimit  = 8_000_000m,
                    Deductible     = 15_000m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Property & fire|Burglary protection|Business stock cover|Natural disaster|Business interruption|Liability cover|Machinery breakdown",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Professional Indemnity",
                    Description    = "Protects professionals and businesses against claims of negligence, errors, or omissions in the delivery of professional services.",
                    Type           = InsuranceType.Business,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 6_500.00m,
                    CoverageLimit  = 5_000_000m,
                    Deductible     = 10_000m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Professional negligence|Errors & omissions|Legal defence costs|Libel & slander|Loss of documents|Court attendance costs",
                    ProviderId     = apa.Id
                },
                new InsurancePlan
                {
                    PlanName       = "Jubilee Group Life (WIBA)",
                    Description    = "Work Injury Benefits Act compliant group life cover protecting employers and employees from workplace injury claims.",
                    Type           = InsuranceType.Business,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 4_500.00m,
                    CoverageLimit  = 3_000_000m,
                    Deductible     = 0m,
                    Rating         = 4.5,
                    IsPopular      = false,
                    Features       = "WIBA Act compliant|Workplace injury cover|Death benefit|Permanent disability|Temporary disability|Medical expenses|Legal compliance",
                    ProviderId     = jubilee.Id
                },

                // ═══════════════════════════════
                // AGRICULTURE INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "CIC Crop Insurance",
                    Description    = "Kenya's market-leading crop insurance protecting farmers against drought, floods, disease outbreaks, and other weather-related losses.",
                    Type           = InsuranceType.Agriculture,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 3_500.00m,
                    CoverageLimit  = 500_000m,
                    Deductible     = 5_000m,
                    Rating         = 4.5,
                    IsPopular      = true,
                    Features       = "Drought protection|Flood damage|Disease outbreak|Pest infestation|Market-leading agricultural insurer|Cooperative member discount|Fast claim assessment",
                    ProviderId     = cic.Id
                },
                new InsurancePlan
                {
                    PlanName       = "CIC Livestock Insurance",
                    Description    = "Compensates livestock owners for losses due to disease, fire, or emergency slaughter — protecting the core asset of pastoral farmers.",
                    Type           = InsuranceType.Agriculture,
                    Tier           = PlanTier.Standard,
                    MonthlyPremium = 2_500.00m,
                    CoverageLimit  = 300_000m,
                    Deductible     = 3_000m,
                    Rating         = 4.4,
                    IsPopular      = false,
                    Features       = "Disease-related deaths|Accidental death|Emergency slaughter|Fire losses|Transit cover|Veterinary fee reimbursement",
                    ProviderId     = cic.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Kilimo Salama Farm Cover",
                    Description    = "Index-based agricultural insurance for smallholder farmers, using weather station data to trigger automatic payouts without requiring farm inspection.",
                    Type           = InsuranceType.Agriculture,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 1_200.00m,
                    CoverageLimit  = 200_000m,
                    Deductible     = 0m,
                    Rating         = 4.3,
                    IsPopular      = false,
                    Features       = "Index-based automatic payout|No farm inspection needed|Weather-station triggered|Drought & excess rainfall|Smallholder farmer focused|Mobile money payout",
                    ProviderId     = apa.Id
                },

                // ═══════════════════════════════
                // MICRO INSURANCE
                // ═══════════════════════════════

                new InsurancePlan
                {
                    PlanName       = "Britam Bima ya Mwananchi",
                    Description    = "Affordable micro medical cover for the everyday Kenyan — inpatient cover from as little as Ksh 100/month via M-PESA.",
                    Type           = InsuranceType.Micro,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 200.00m,
                    CoverageLimit  = 100_000m,
                    Deductible     = 500m,
                    Rating         = 4.3,
                    IsPopular      = true,
                    Features       = "Pay via M-PESA|Inpatient cover|Low entry premium|Daily hospital cash benefit|Last expense cover|Easy mobile enrollment",
                    ProviderId     = britam.Id
                },
                new InsurancePlan
                {
                    PlanName       = "CIC Seniors Mediplan",
                    Description    = "Specially designed health coverage for senior citizens — providing inpatient and outpatient benefits at affordable rates for those aged 60+.",
                    Type           = InsuranceType.Micro,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 1_800.00m,
                    CoverageLimit  = 300_000m,
                    Deductible     = 1_000m,
                    Rating         = 4.2,
                    IsPopular      = false,
                    Features       = "Age 60+ focused|Inpatient & outpatient|Chronic disease management|Last expense cover|Dental cover|Optical cover|Low premium for seniors",
                    ProviderId     = cic.Id
                },
                new InsurancePlan
                {
                    PlanName       = "APA Last Expense Cover",
                    Description    = "A low-cost funeral and last expense cover ensuring your family has immediate funds to manage funeral arrangements without financial strain.",
                    Type           = InsuranceType.Micro,
                    Tier           = PlanTier.Basic,
                    MonthlyPremium = 350.00m,
                    CoverageLimit  = 150_000m,
                    Deductible     = 0m,
                    Rating         = 4.1,
                    IsPopular      = false,
                    Features       = "Fast payout within 48 hours|Covers entire family|Funeral expenses|No medical exam required|Mobile enrollment|M-PESA payment accepted",
                    ProviderId     = apa.Id
                }
            };

            await context.InsurancePlans.AddRangeAsync(plans);
            await context.SaveChangesAsync();
        }

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { "Admin", "User" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        public static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration config)
        {
            var email    = config["AdminSeed:Email"]    ?? "admin@insurancehub.co.ke";
            var password = config["AdminSeed:Password"] ?? "Admin@123!";

            if (await userManager.FindByEmailAsync(email) is not null)
                return;

            var user = new ApplicationUser
            {
                UserName       = email,
                Email          = email,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, "Admin");
        }

        /// <summary>
        /// Bootstraps the singleton AppSettings row from the EmailSettings__* env vars on first
        /// run only. After this, /admin/settings and ISettingsService are the source of truth —
        /// existing deployments keep working unchanged, editing Settings just takes over from there.
        /// </summary>
        public static async Task SeedAppSettingsAsync(
            ApplicationDbContext db,
            IOptions<EmailSettings> emailOptions,
            IDataProtectionProvider dataProtectionProvider)
        {
            if (await db.AppSettings.AnyAsync()) return;

            var email     = emailOptions.Value;
            var protector = dataProtectionProvider.CreateProtector("AppSettings.Secrets");

            db.AppSettings.Add(new AppSettings
            {
                AgentContactEmail  = email.AgentEmail,
                SmtpHost           = email.SmtpHost,
                SmtpPort           = email.SmtpPort,
                SmtpUseSsl         = email.UseSsl,
                SmtpSenderEmail    = email.SenderEmail,
                SmtpSenderName     = email.SenderName,
                SmtpSenderPassword = string.IsNullOrEmpty(email.SenderPassword)
                                        ? string.Empty
                                        : protector.Protect(email.SenderPassword),
                SmtpAgentEmail     = email.AgentEmail,
                LeadApiKey         = protector.Protect(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)))
            });

            await db.SaveChangesAsync();
        }
    }
}
