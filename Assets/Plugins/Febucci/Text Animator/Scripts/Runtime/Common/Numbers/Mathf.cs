namespace Febucci.Numbers
{
    public static class Mathf
    {
        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static float Clamp01(float value) => Clamp(value, 0, 1);

        public static float Lerp(float min, float max, float t)
        {
            return min + (max - min) * Clamp(t, 0, 1);
        }
    }
}