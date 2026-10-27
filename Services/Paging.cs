namespace Insurance_Hub.Services
{
    /// <summary>Shared paging for long admin lists (see Views/Shared/_Pager.cshtml).</summary>
    public static class Paging
    {
        public const int PageSize = 50;

        public static int Normalize(int? page) => page is int p && p > 0 ? Math.Min(p, 10_000) : 1;

        public static int TotalPages(int total) => Math.Max(1, (total + PageSize - 1) / PageSize);
    }
}
