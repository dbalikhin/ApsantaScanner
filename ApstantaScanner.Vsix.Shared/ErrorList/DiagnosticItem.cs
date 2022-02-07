namespace ApstantaScanner.Vsix.Shared.ErrorList
{
    public class DiagnosticItem
    {
        public string ErrorCode { get; set; }
        public string ErrorText { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string ProjectName { get; set; }

    }
}
