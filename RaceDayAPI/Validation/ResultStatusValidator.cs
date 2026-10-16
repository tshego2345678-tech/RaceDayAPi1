using System;
using System.Linq;

namespace RaceDayApi.Validation
{
    public static class ResultStatusValidator
    {
        private static readonly string[] AllowedStatuses =
        {
            "Finished",
            "DNF",
            "DSQ"
        };

        public static bool IsValidStatus(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) &&
                   AllowedStatuses.Contains(
                       status,
                       StringComparer.OrdinalIgnoreCase);
        }
    }
}