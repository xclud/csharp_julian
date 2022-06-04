namespace System;

public static class DateTimeExtensions
{
    public static Julian ToJulian(this DateTime value)
    {
        return Julian.FromDateTime(value);
    }
}
