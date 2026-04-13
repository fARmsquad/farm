namespace FarmSimVR.Core.Story
{
    public sealed class StoryPackageValidationResult
    {
        public StoryPackageValidationResult(bool isValid, string[] errors)
        {
            IsValid = isValid;
            Errors = errors ?? System.Array.Empty<string>();
        }

        public bool IsValid { get; }
        public string[] Errors { get; }
        public string FirstError => Errors.Length > 0 ? Errors[0] : string.Empty;
    }
}
