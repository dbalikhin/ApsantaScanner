namespace VisualStudio2022.Auth
{
    public class GithubAuthStatusChangedEventArgs : EventArgs
    {
        public AuthStatus OldStatus { get; }

        public AuthStatus NewStatus { get; }

        public GithubAuthStatusChangedEventArgs(AuthStatus oldStatus, AuthStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    public class GithubAuthDeviceCodeReceivedEventArgs : EventArgs
    {
        public string UserCode { get; }

        public GithubAuthDeviceCodeReceivedEventArgs(string userCode)
        {
            UserCode = userCode;
        }
    }

    public class GithubAuthUserTokenReceivedEventArgs : EventArgs
    {
        public string UserToken { get; }

        public GithubAuthUserTokenReceivedEventArgs(string userToken)
        {
            UserToken = userToken;
        }
    }
}
