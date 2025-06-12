using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using ElectroStore.Properties;
using ElectroStore1;

namespace ElectroStore
{
    public static class AuthService
    {
        public static void ClearSavedData()
        {
            Settings.Default.SavedUsername = string.Empty;
            Settings.Default.SavedPasswordHash = string.Empty;
            Settings.Default.RememberMe = false;
            Settings.Default.Save();
        }

        public static string GetSavedUsername() => Settings.Default.SavedUsername;
        public static bool HasSavedUsername() => Settings.Default.RememberMe && !string.IsNullOrEmpty(Settings.Default.SavedUsername);
    }
}