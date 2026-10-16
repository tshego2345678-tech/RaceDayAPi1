using RaceDayApi.Validation;

namespace RaceDayApi.Tests_.Helpers
{
    public static class ResultStatusValidator
    {
        public static bool IsValidStatus(string? status)
        {
            return RaceDayApi.Validation.ResultStatusValidator
                .IsValidStatus(status);
        }
    }
}