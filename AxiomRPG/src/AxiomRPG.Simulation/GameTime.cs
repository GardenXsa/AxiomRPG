namespace AxiomRPG.Simulation;

public class GameTime
{
    public int TotalTicks { get; private set; }
    public int Minute { get; private set; }
    public int Hour { get; private set; }
    public int Day { get; private set; }
    public int Month { get; private set; }
    public int Year { get; private set; }

    public const int TicksPerMinute = 10;
    public const int MinutesPerHour = 60;
    public const int HoursPerDay = 24;
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;

    public float DayProgress => (Hour * MinutesPerHour + Minute) / (float)(HoursPerDay * MinutesPerHour);
    public bool IsDaytime => Hour >= 6 && Hour < 20;
    public bool IsNighttime => !IsDaytime;
    public string TimeOfDay => Hour switch
    {
        >= 5 and < 8 => "dawn",
        >= 8 and < 12 => "morning",
        >= 12 and < 14 => "midday",
        >= 14 and < 17 => "afternoon",
        >= 17 and < 20 => "dusk",
        >= 20 and < 23 => "evening",
        _ => "night"
    };

    public GameTime(int year = 1, int month = 1, int day = 1, int hour = 8, int minute = 0)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
    }

    public void AdvanceTicks(int ticks)
    {
        TotalTicks += ticks;
        var totalMinutes = ticks / TicksPerMinute;
        AdvanceMinutes(totalMinutes);
    }

    public void AdvanceMinutes(int minutes)
    {
        Minute += minutes;
        while (Minute >= MinutesPerHour)
        {
            Minute -= MinutesPerHour;
            Hour++;
        }
        while (Hour >= HoursPerDay)
        {
            Hour -= HoursPerDay;
            Day++;
        }
        while (Day > DaysPerMonth)
        {
            Day -= DaysPerMonth;
            Month++;
        }
        while (Month > MonthsPerYear)
        {
            Month -= MonthsPerYear;
            Year++;
        }
    }

    public override string ToString() => $"Year {Year}, Month {Month}, Day {Day} - {Hour:D2}:{Minute:D2} ({TimeOfDay})";
}
