
using Supabase;

namespace SmartXWPF.Services
{
    public class AuthService
    {
        private readonly Supabase.Client _client;

        public AuthService(string supabaseUrl, string supabaseKey)
        {
            _client = new Supabase.Client(
                supabaseUrl,
                supabaseKey,
                new SupabaseOptions
                {
                    AutoConnectRealtime = false
                });
        }

        public async Task InitializeAsync()
        {
            await _client.InitializeAsync();
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                await _client.Auth.SignIn(email, password);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string email, string password)
        {
            try
            {
                await _client.Auth.SignUp(email, password);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            await _client.Auth.SignOut();
        }
    }
}
