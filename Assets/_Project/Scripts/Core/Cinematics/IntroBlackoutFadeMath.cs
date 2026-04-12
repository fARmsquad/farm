namespace FarmSimVR.Core.Cinematics
{
    /// <summary>
    /// Alpha for a linear fade to black driven by timeline clock time.
    /// </summary>
    public static class IntroBlackoutFadeMath
    {
        public static float ComputeAlpha(double directorTime, double fadeStartTime, double fadeDuration)
        {
            if (fadeDuration <= 0d)
                return directorTime >= fadeStartTime ? 1f : 0f;

            if (directorTime < fadeStartTime)
                return 0f;

            if (directorTime >= fadeStartTime + fadeDuration)
                return 1f;

            return (float)((directorTime - fadeStartTime) / fadeDuration);
        }
    }
}
