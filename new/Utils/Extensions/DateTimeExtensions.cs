namespace IvoEngine.Extensions;

public static class DateTimeExtensions
{
    public static DateOnly ToDate(this DateTime dateTime)
    {
        return DateOnly.FromDateTime(dateTime);
    }
    public static TimeOnly ToTime(this DateTime dateTime)
    {
        return TimeOnly.FromDateTime(dateTime);
    }

}


