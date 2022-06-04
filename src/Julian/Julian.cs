namespace System;

/// <summary>
/// Julian date.
/// </summary>
public readonly struct Julian
{
    /// <summary>
    /// The Julian date.
    /// </summary>
    public readonly double Value;

    public double FromJan0_12h_1900() { return Value - EPOCH_JAN0_12H_1900; }
    public double FromJan1_00h_1900() { return Value - EPOCH_JAN1_00H_1900; }
    public double FromJan1_12h_1900() { return Value - EPOCH_JAN1_12H_1900; }
    public double FromJan1_12h_2000() { return Value - EPOCH_JAN1_12H_2000; }

    private const double EPOCH_JAN0_12H_1900 = 2415020.0; // Dec 31.5 1899 = Dec 31 1899 12h UTC
    private const double EPOCH_JAN1_00H_1900 = 2415020.5; // Jan  1.0 1900 = Jan  1 1900 00h UTC
    private const double EPOCH_JAN1_12H_1900 = 2415021.0; // Jan  1.5 1900 = Jan  1 1900 12h UTC
    private const double EPOCH_JAN1_12H_2000 = 2451545.0; // Jan  1.5 2000 = Jan  1 2000 12h UTC

    private const double HoursPerDay = 24.0;          // Hours per day   (solar)
    private const double MinutesPerDay = 1440.0;        // Minutes per day (solar)
    private const double SecondsPerDay = 86400.0;       // Seconds per day (solar)
    private const double OmegaE = 1.00273790934; // Earth rotation per sidereal day


    #region Construction

    /// <summary>
    /// Create a Julian date object from a DateTime object. The time
    /// contained in the DateTime object is assumed to be UTC.
    /// </summary>
    /// <param name="value">The UTC time to convert.</param>
    public static Julian FromDateTime(DateTime value)
    {
        var utc = value.ToUniversalTime();

        double dayOfYear = utc.DayOfYear +
         ((utc.Hour +
         ((utc.Minute +
         ((utc.Second + (utc.Millisecond / 1000.0)) / 60.0)) / 60.0)) / 24.0);

        // The last day of a leap year is day 366
        if (dayOfYear < 1.0 || dayOfYear >= 367.0)
        {
            throw new ArgumentOutOfRangeException("doy");
        }

        var year = utc.Year;
        // Now calculate Julian date
        // Ref: "Astronomical Formulae for Calculators", Jean Meeus, pages 23-25

        year--;

        // Centuries are not leap years unless they divide by 400
        int A = year / 100;
        int B = 2 - A + (A / 4);

        double jan01 = (int)(365.25 * year) +
                       (int)(30.6001 * 14) +
                       1720994.5 + B;

        return new Julian(jan01 + dayOfYear);
    }

    public Julian(double value)
    {
        Value = value;
    }

    /// <summary>
    /// Create a Julian date object given a year and day-of-year.
    /// </summary>
    /// <param name="year">The year, including the century (i.e., 2022).</param>
    /// <param name="dayOfYear">Day of year (1 means January 1, etc.).</param>
    /// <remarks>
    /// The fractional part of the day value is the fractional portion of
    /// the day.
    /// Examples: 
    ///    day = 1.0  Jan 1 00h
    ///    day = 1.5  Jan 1 12h
    ///    day = 2.0  Jan 2 00h
    /// </remarks>
    public Julian(int year, double dayOfYear)
    {
        // The last day of a leap year is day 366
        if (dayOfYear < 1.0 || dayOfYear >= 367.0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfYear));
        }

        // Now calculate Julian date
        // Ref: "Astronomical Formulae for Calculators", Jean Meeus, pages 23-25

        year--;

        // Centuries are not leap years unless they divide by 400
        int A = year / 100;
        int B = 2 - A + (A / 4);

        double jan01 = (int)(365.25 * year) +
                       (int)(30.6001 * 14) +
                       1720994.5 + B;

        Value = jan01 + dayOfYear;
    }

    #endregion

    public Julian AddDays(double days) { return new Julian(Value + days); }
    public Julian AddHours(double hours) { return new Julian(Value + (hours / HoursPerDay)); }
    public Julian AddMinutes(double minutes) { return new Julian(Value + (minutes / MinutesPerDay)); }
    public Julian AddSeconds(double seconds) { return new Julian(Value + (seconds / SecondsPerDay)); }

    /// <summary>
    /// Calculates the time difference between two Julian dates.
    /// </summary>
    /// <param name="left">Julian date.</param>
    /// <param name="right">Julian date.</param>
    /// <returns>
    /// A TimeSpan representing the time difference between the two dates.
    /// </returns>
    public static TimeSpan operator -(Julian left, Julian right)
    {
        const double TICKS_PER_DAY = 8.64e11; // 1 tick = 100 nanoseconds
        return new TimeSpan((long)((left.Value - right.Value) * TICKS_PER_DAY));
    }

    /// <summary>
    /// Calculate Greenwich Mean Sidereal Time for the Julian date.
    /// </summary>
    /// <returns>
    /// The angle, in radians, measuring eastward from the Vernal Equinox to
    /// the prime meridian. This angle is also referred to as "ThetaG" 
    /// (Theta GMST).
    /// </returns>
    public double ToGmst()
    {
        // References:
        //    The 1992 Astronomical Almanac, page B6.
        //    Explanatory Supplement to the Astronomical Almanac, page 50.
        //    Orbital Coordinate Systems, Part III, Dr. T.S. Kelso, 
        //       Satellite Times, Nov/Dec 1995

        double UT = (Value + 0.5) % 1.0;
        double TU = (FromJan1_12h_2000() - UT) / 36525.0;

        double GMST = 24110.54841 + (TU *
                      (8640184.812866 + (TU * (0.093104 - (TU * 6.2e-06)))));

        GMST = (GMST + (SecondsPerDay * OmegaE * UT)) % SecondsPerDay;

        if (GMST < 0.0)
        {
            GMST += SecondsPerDay;  // "wrap" negative modulo value
        }

        return Math.PI * 2 * (GMST / SecondsPerDay);
    }

    /// <summary>
    /// Calculate Local Mean Sidereal Time for this Julian date at the given
    /// longitude.
    /// </summary>
    /// <param name="longitude">The longitude, in radians, measured west from Greenwich.</param>
    /// <returns>
    /// The angle, in radians, measuring eastward from the Vernal Equinox to
    /// the given longitude.
    /// </returns>
    public double ToLmst(double longitude)
    {
        return (ToGmst() + longitude) % Math.PI * 2;
    }

    /// <summary>
    /// Returns a UTC DateTime object that corresponds to this Julian date.
    /// </summary>
    /// <returns>A DateTime object in UTC.</returns>
    public DateTime ToDateTime()
    {
        double d2 = Value + 0.5;
        int Z = (int)d2;
        int alpha = (int)((Z - 1867216.25) / 36524.25);
        int A = Z + 1 + alpha - (alpha / 4);
        int B = A + 1524;
        int C = (int)((B - 122.1) / 365.25);
        int D = (int)(365.25 * C);
        int E = (int)((B - D) / 30.6001);

        // For reference: the fractional day of the month can be
        // calculated as follows:
        //
        // double day = B - D - (int)(30.6001 * E) + F;

        int month = (E <= 13) ? (E - 1) : (E - 13);
        int year = (month >= 3) ? (C - 4716) : (C - 4715);

        Julian jdJan01 = new Julian(year, 1.0);
        double dayOfYear = Value - jdJan01.Value; // zero-relative

        DateTime dtJan01 = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return dtJan01.AddDays(dayOfYear);
    }
}
