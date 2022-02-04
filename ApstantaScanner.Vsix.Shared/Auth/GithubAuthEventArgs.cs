namespace ApstantaScanner.Vsix.Shared.Auth
{
    public class GithubAuthStatusChangedEventArgs : EventArgs
    {
        public AuthStatus OldStatus { get; }

        public AuthStatus NewStatus { get; }

        // Temporary code for auth device flow
        public string UserCode { get; }

        // Access token
        public string UserToken { get; }

        public GithubAuthStatusChangedEventArgs(AuthStatus oldStatus, AuthStatus newStatus, string userCode, string userToken)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            UserCode = userCode;
            UserToken = userToken;
        }
    }
}
