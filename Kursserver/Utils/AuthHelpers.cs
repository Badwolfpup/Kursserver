namespace Kursserver.Utils
{
    public static class AuthHelpers
    {
        public static bool IsLockedOut(DateTime lockoutStart, DateTime now)
            => (now - lockoutStart).TotalMinutes < 15;

        public static int GetTokenExpiryDays(int authLevel)
            => authLevel <= 2 ? 30 : 6;
    }
}
