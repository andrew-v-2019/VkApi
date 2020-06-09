namespace VKApi.BL.Models.Users
{
    public class AgeRange
    {
        public AgeRange(int min, int max)
        {
            Max = max;
            Min = min;
        }

        public int Min { get; set; }
        public int Max { get; set; }
    }
}
