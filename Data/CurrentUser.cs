using Microsoft.AspNetCore.Http;

namespace Lib_Mgmt.Data
{
    /// <summary>
    /// Thin wrapper over HttpContext.Session for the signed-in user.
    /// The session is populated in AccountController.Login and read by every
    /// portal action so the controller always knows "who is asking".
    /// </summary>
    public static class CurrentUser
    {
        // Session keys (kept short — they are part of the serialized payload).
        private const string KeyId    = "uid";
        private const string KeyRole  = "role";    // "LIBRARIAN" | "MEMBER"
        private const string KeyName  = "fname";
        private const string KeyUser  = "uname";
        private const string KeyCode  = "ucode";   // LIBRARIAN_CODE / MEMBER_CODE

        public static void SignIn(ISession s, int id, string role,
                                  string fullName, string username, string code)
        {
            s.SetInt32(KeyId, id);
            s.SetString(KeyRole, role ?? "");
            s.SetString(KeyName, fullName ?? "");
            s.SetString(KeyUser, username ?? "");
            s.SetString(KeyCode, code ?? "");
        }

        public static void SignOut(ISession s) => s.Clear();

        public static bool IsSignedIn(ISession s) => s.GetInt32(KeyId).HasValue;

        public static int Id(ISession s) => s.GetInt32(KeyId) ?? 0;
        public static string Role(ISession s) => s.GetString(KeyRole) ?? "";
        public static string FullName(ISession s) => s.GetString(KeyName) ?? "";
        public static string Username(ISession s) => s.GetString(KeyUser) ?? "";
        public static string Code(ISession s) => s.GetString(KeyCode) ?? "";

        public static bool IsLibrarian(ISession s) => Role(s) == "LIBRARIAN";
        public static bool IsMember(ISession s) => Role(s) == "MEMBER";
    }
}
