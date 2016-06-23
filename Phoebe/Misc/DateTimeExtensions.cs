using System;

namespace Toggl.Phoebe
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime UnixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static TimeSpan? ToUnix(this DateTime? val)
        {
            if (val == null)
            {
                return null;
            }
            return val.Value.ToUnix();
        }

        public static TimeSpan ToUnix(this DateTime val)
        {
            return val.ToUtc() - UnixStart;
        }

        public static DateTime? ToUtc(this DateTime? val)
        {
            if (val == null)
            {
                return null;
            }
            return val.Value.ToUtc();
        }

        public static DateTime ToUtc(this DateTime val)
        {
            if (val.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(val, DateTimeKind.Utc);
            }
            else
            {
                return val.ToUniversalTime();
            }
        }

        public static DateTime? Truncate(this DateTime? val, long percisionTicks)
        {
            if (val == null)
            {
                return null;
            }
            return val.Value.Truncate(percisionTicks);
        }

        public static DateTime Truncate(this DateTime val, long precisionTicks)
        {
            return val.AddTicks(- (val.Ticks % precisionTicks));
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(-1 * diff).Date;
        }

        public static TimeSpan Truncate(this TimeSpan val, long percisionTicks)
        {
            return new TimeSpan(val.Ticks - (val.Ticks % percisionTicks));
        }

        public static DateTime AbsoluteStart(this DateTime dateTime)
        {
            return dateTime.Date;
        }

        public static DateTime AbsoluteEnd(this DateTime dateTime)
        {
            return AbsoluteStart(dateTime).AddDays(1).AddTicks(-1);
        }

        public static bool IsMinValue(this DateTime dateTime)
        {
            return (dateTime - DateTime.MinValue).Ticks < 10;
        }
    }
}

