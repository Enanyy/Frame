/////////////////////////////////////////////////////////////////////////////////
//
//	vp_TimeUtility.cs
//	?VisionPunk, Minea Softworks. All Rights Reserved.
//
//	description:	vp_TimeUtility contains static utility methods for performing
//					common time related game programming tasks
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;


public static class vp_TimeUtility
{

    /// <summary>
    /// represents a time measured in standard units
    /// </summary>
    public struct Units
    {

        public int hours;
        public int minutes;
        public int seconds;
        public int deciSeconds;		// a.k.a. 'tenths of a second'
        public int centiSeconds;	// a.k.a. 'hundredths of a second'
        public int milliSeconds;

    }


    /// <summary>
    /// takes a floating point time in seconds and returns a
    /// struct representing that time divided into standard units.
    /// the value stored in each variable represents a fraction
    /// in units of the previous larger time component
    /// </summary>
    public static Units TimeToUnits(float timeInSeconds)
    {

        Units iTime = new Units();

        iTime.hours = ((int)timeInSeconds) / 3600;
        iTime.minutes = (((int)timeInSeconds) - (iTime.hours * 3600)) / 60;
        iTime.seconds = ((int)timeInSeconds) % 60;

        iTime.deciSeconds = (int)((timeInSeconds - iTime.seconds) * 10) % 60;
        iTime.centiSeconds = (int)((timeInSeconds - iTime.seconds) * 100 % 600);
        iTime.milliSeconds = (int)((timeInSeconds - iTime.seconds) * 1000 % 6000);

        return iTime;

    }


    /// <summary>
    /// takes a 'Units' struct and returns a floating point time in
    /// seconds.
    /// </summary>
    public static float UnitsToSeconds(Units units)
    {

        float seconds = 0.0f;

        seconds += units.hours * 3600;
        seconds += units.minutes * 60;
        seconds += units.seconds;

        seconds += (float)units.deciSeconds * 0.1f;
        seconds += (float)(units.centiSeconds / 100);
        seconds += (float)(units.milliSeconds / 1000);

        return seconds;

    }


    /// <summary>
    /// takes a floating point time in seconds and returns a
    /// string with the time formatted as a configurable list
    /// of standard time units, delimited by a char of choice.
    /// this is useful for digital time displays such as those
    /// typically found in racing games
    /// </summary>
    public static string TimeToString(float timeInSeconds, bool showHours, bool showMinutes, bool showSeconds,
                                        bool showTenths, bool showHundredths, bool showMilliSeconds,
                                        char delimiter = ':')
    {

        Units iTime = TimeToUnits(timeInSeconds);

        string hours = (iTime.hours < 10) ? "0" + iTime.hours.ToString() : iTime.hours.ToString();
        string minutes = (iTime.minutes < 10) ? "0" + iTime.minutes.ToString() : iTime.minutes.ToString();
        string seconds = (iTime.seconds < 10) ? "0" + iTime.seconds.ToString() : iTime.seconds.ToString();
        string deciSeconds = iTime.deciSeconds.ToString();
        string centiSeconds = (iTime.centiSeconds < 10) ? "0" + iTime.centiSeconds.ToString() : iTime.centiSeconds.ToString();
        string milliSeconds = (iTime.milliSeconds < 100) ? "0" + iTime.milliSeconds.ToString() : iTime.milliSeconds.ToString();
        milliSeconds = (iTime.milliSeconds < 10) ? "0" + milliSeconds : milliSeconds;

        return ((showHours ? hours : "") +
            (showMinutes ? delimiter + minutes : "") +
            (showSeconds ? delimiter + seconds : "") +
            (showTenths ? delimiter + deciSeconds : "") +
            (showHundredths ? delimiter + centiSeconds : "") +
            (showMilliSeconds ? delimiter + milliSeconds : "")).TrimStart(delimiter);

    }


    /// <summary>
    /// takes a 'System.DateTime' object and returns a string
    /// with the time formatted as a configurable list of standard
    /// time units, delimited by a char of choice
    /// </summary>
    public static string SystemTimeToString(System.DateTime systemTime, bool showHours, bool showMinutes, bool showSeconds,
                                        bool showTenths, bool showHundredths, bool showMilliSeconds,
                                        char delimiter = ':')
    {
        return TimeToString(SystemTimeToSeconds(systemTime), showHours, showMinutes, showSeconds, showTenths, showHundredths, showMilliSeconds, delimiter);
    }


    // overload defaulting to the current system time
    public static string SystemTimeToString(bool showHours, bool showMinutes, bool showSeconds,
                                        bool showTenths, bool showHundredths, bool showMilliSeconds,
                                        char delimiter = ':')
    {
        return SystemTimeToString(System.DateTime.Now, showHours, showMinutes, showSeconds, showTenths, showHundredths, showMilliSeconds, delimiter);
    }


    /// <summary>
    /// takes a 'System.DateTime' object and returns a struct
    /// representing that time divided into standard units.
    /// the value stored in each variable represents a fraction
    /// in units of the previous larger time component
    /// </summary>
    public static Units SystemTimeToUnits(System.DateTime systemTime)
    {

        Units iTime = new Units();

        iTime.hours = systemTime.Hour;
        iTime.minutes = systemTime.Minute;
        iTime.seconds = systemTime.Second;
        iTime.deciSeconds = (int)(systemTime.Millisecond / 100.0f);
        iTime.centiSeconds = (int)(systemTime.Millisecond / 10);
        iTime.milliSeconds = systemTime.Millisecond;

        return iTime;

    }


    // overload defaulting to the current system time
    public static Units SystemTimeToUnits()
    {
        return SystemTimeToUnits(System.DateTime.Now);
    }


    /// <summary>
    /// takes a 'System.DateTime' object and returns the total
    /// number of seconds represented by the object
    /// </summary>
    public static float SystemTimeToSeconds(System.DateTime systemTime)
    {

        return UnitsToSeconds(SystemTimeToUnits(systemTime));

    }


    // overload defaulting to the current system time
    public static float SystemTimeToSeconds()
    {
        return SystemTimeToSeconds(System.DateTime.Now);
    }


    /// <summary>
    /// converts a floating point time in seconds to an angle in
    /// degrees, typically for use by clocks and meters in both 2d
    /// and 3d. each optional boolean represents the resolution at
    /// which to represent the time value. this determines how
    /// "smooth" the clock hand will move. by default the method
    /// will return an angle corresponding to the current amount
    /// of seconds in the current minute at millisecond resolution
    /// </summary>
    public static float TimeToDegrees(float seconds, bool includeHours = false, bool includeMinutes = false, bool includeSeconds = true, bool includeMilliSeconds = true)
    {

        Units iTime = TimeToUnits(seconds);

        // hours @ second-resolution
        if (includeHours && includeMinutes && includeSeconds)
            return HoursToDegreesInternal(iTime.hours, iTime.minutes, iTime.seconds);

        // hours @ minute-resolution
        if (includeHours && includeMinutes)
            return HoursToDegreesInternal(iTime.hours, iTime.minutes);

        // minutes @ millisecond-resolution
        if (includeMinutes && includeSeconds && includeMilliSeconds)
            return MinutesToDegreesInternal(iTime.minutes, iTime.seconds, iTime.milliSeconds);

        // minutes @ second-resolution
        if (includeMinutes && includeSeconds)
            return MinutesToDegreesInternal(iTime.minutes, iTime.seconds);

        // DEFAULT: seconds @ millisecond-resolution
        if (includeSeconds && includeMilliSeconds)
            return SecondsToDegreesInternal(iTime.seconds, iTime.milliSeconds);

        // hours @ hour-resolution
        if (includeHours)
            return HoursToDegreesInternal(iTime.hours);

        // minutes @ minute-resolution
        if (includeMinutes)
            return MinutesToDegreesInternal(iTime.minutes);

        // seconds @ second-resolution
        if (includeSeconds)
            return TimeToDegrees(iTime.seconds);

        // milliseconds @ millisecond-resolution
        if (includeMilliSeconds)
            return MilliSecondsToDegreesInternal(iTime.milliSeconds);

        UnityEngine.Debug.LogError("Error: (vp_TimeUtility.TimeToDegrees) This combination of time units is not supported.");

        return 0.0f;

    }


    /// <summary>
    /// converts a 'System.DateTime' object into three degree angles:
    /// 'x' representing the hour-hand, 'y' representing the minute-hand,
    /// and 'z' representing the second-hand of a classic analog clock,
    /// respectively. omit the 'time' parameter to default to the current
    /// system time
    /// </summary>
    public static Vector3 SystemTimeToDegrees(System.DateTime time, bool smooth = true)
    {

        return new Vector3(
            HoursToDegreesInternal(time.Hour, smooth ? time.Minute : 0.0f, smooth ? time.Second : 0.0f),
            MinutesToDegreesInternal(time.Minute, smooth ? time.Second : 0.0f, smooth ? time.Millisecond : 0.0f),
            SecondsToDegreesInternal(time.Second, smooth ? time.Millisecond : 0.0f));

    }

    // overload defaulting to the current system time
    public static Vector3 SystemTimeToDegrees(bool smooth = true)
    {
        return SystemTimeToDegrees(System.DateTime.Now, smooth);
    }


    /// <summary>
    /// converts a time in hours to an angle in degrees, assuming
    /// a traditional analog clock face containing 12 hours.
    /// optional booleans determine the sub-hour resolution
    /// </summary>
    private static float HoursToDegreesInternal(float hours, float minutes = 0.0f, float seconds = 0.0f)
    {
        return (hours * 30.0f) + (minutes * 0.5f) + (seconds * 0.0083333333f);
    }


    /// <summary>
    /// converts a time in minutes to an angle in degrees, assuming
    /// a traditional analog clock face containing 60 minutes.
    /// optional booleans determine the sub-minute resolution
    /// </summary>
    private static float MinutesToDegreesInternal(float minutes, float seconds = 0.0f, float milliSeconds = 0.0f)
    {
        return (minutes * 6.0f) + (seconds * 0.1f) + (milliSeconds * 0.0001f);
    }


    /// <summary>
    /// converts a time in seconds to an angle in degrees, assuming
    /// a traditional analog clock face containing 60 seconds.
    /// optional booleans determine the sub-second resolution
    /// </summary>
    private static float SecondsToDegreesInternal(float seconds, float milliSeconds = 0.0f)
    {
        return (seconds * 6.0f) + (milliSeconds * 0.006f);
    }


    /// <summary>
    /// converts a time in milliseconds to an angle in degrees,
    /// assuming a clock face containing 1000 milliseconds
    /// </summary>
    private static float MilliSecondsToDegreesInternal(float milliSeconds)
    {
        return milliSeconds * 0.36f;
    }


}
