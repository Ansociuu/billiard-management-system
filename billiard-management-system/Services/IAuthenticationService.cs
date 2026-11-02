using billiard_management_system.Models;

namespace billiard_management_system.Services
{
    /// <summary>
    /// Service quản lý phiên đăng nhập người dùng hiện tại
    /// </summary>
    public interface IAuthenticationService
    {
        UserSession CurrentUser { get; }
        bool IsAuthenticated { get; }
        void SetCurrentUser(UserSession userSession);
        void Logout();
    }

    public class AuthenticationService : IAuthenticationService
    {
        public UserSession CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public void SetCurrentUser(UserSession userSession)
        {
            CurrentUser = userSession;
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
