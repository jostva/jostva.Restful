using System;

namespace jostva.Restful.API.Helpers
{
    public static class DateTimeOffsetExtensions
    {

        public static int GetCurrentAge(this DateTimeOffset dateTimeOffset)
        {
            DateTime currentDate = DateTime.UtcNow;
            int age = currentDate.Year - dateTimeOffset.Year;

            if (currentDate < dateTimeOffset.AddYears(age))
            {
                age--;
            }

            return age;
        }
    }
}