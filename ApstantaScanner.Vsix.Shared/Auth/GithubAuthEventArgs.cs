namespace ApstantaScanner.Vsix.Shared.Auth
{
    public class GithubAuthStatusChangedEventArgs : EventArgs
    {
        public AuthStatus OldStatus { get; }

        public AuthStatus NewStatus { get; }

        /// <summary>
        /// Temporary code for auth device flow
        /// </summary>
        public string UserCode { get; }

        /// <summary>
        /// Access token
        /// </summary>
        public string UserToken { get; }

        public string ErrorMessage { get; }

        public GithubAuthStatusChangedEventArgs(AuthStatus oldStatus, AuthStatus newStatus, string userCode, string userToken, string errorMessage)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            UserCode = userCode;
            UserToken = userToken;
            ErrorMessage = errorMessage;
        }
    }
}
