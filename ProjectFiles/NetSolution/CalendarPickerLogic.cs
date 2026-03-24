#region Using directives
using System;
using System.Globalization;
using FTOptix.NetLogic;
using UAManagedCore;
using FTOptix.DataLogger;
using FTOptix.Recipe;
using FTOptix.AuditSigning;
using FTOptix.RecipeX;
using FTOptix.OPCUAServer;
#endregion

public class CalendarPickerLogic : BaseNetLogic
{
    public override void Start()
    {
        // Init all global variables
        selectedDayVariable = LogicObject.GetVariable("SelDay");
        actualDayVariable = LogicObject.GetVariable("ActDay");
        selectedMonthYearVariable = LogicObject.GetVariable("SelMonthYear");
        selectedActMonthVariable = LogicObject.GetVariable("SelActMonth");
        isTodayVariable = LogicObject.GetVariable("IsToday");
        var outputDateVariable = Owner.GetAlias("OutputDate");
        inputDateVariable = outputDateVariable.GetVariable("SelectedDate");
        // Draw the calendar
        DrawCalendar();
    }

    public override void Stop()
    {
        // Nothing to do here
    }

    /// <summary>
    /// This method changes the month displayed in the calendar by a specified number of months.
    /// It updates the selected month and year variables, checks if the selected date is today,
    /// and calls the SundayToSaturday() method to update the day labels.
    /// </summary>
    /// <remarks>
    /// The method takes an integer parameter <paramref name="monthsToAdd"/> which can be positive or negative.
    /// Positive values move to the next month, while negative values move to the previous month.
    /// </remarks>
    /// <param name="monthsToAdd">The number of months to add (or subtract) from the current month.</param>
    [ExportMethod]
    public void ChangeMonth(int monthsToAdd)
    {
        // Get the active month from SelMonthYear
        var currentlySelectedDate = GetSelectedDate();
        // Moving to next or previous month
        currentlySelectedDate = currentlySelectedDate.AddMonths(monthsToAdd);
        // Replace new month, year on variable
        selectedMonthYearVariable.Value = currentlySelectedDate.Date.ToString("MMMM yyyy");

        DateTime selectedDateTime = inputDateVariable.Value;
        selectedActMonthVariable.Value = currentlySelectedDate.Month == selectedDateTime.Month &&
                                         currentlySelectedDate.Year == selectedDateTime.Year;

        var dateTimeNow = DateTime.Now;

        isTodayVariable.Value = currentlySelectedDate.Month == dateTimeNow.Month &&
                                currentlySelectedDate.Year == dateTimeNow.Year &&
                                currentlySelectedDate.Day == dateTimeNow.Day;

        SundayToSaturday();
    }

    /// <summary>
    /// This method updates the day labels in the calendar to start from Sunday to Saturday.
    /// It creates an array of strings representing the days of the month, starting from the first day of the week.
    /// </summary>
    private void SundayToSaturday()
    {
        // Here I want to create an array of days where data starts from the first day of the week
        var currentlySelectedDate = GetSelectedDate();
        int daysInMonth = DateTime.DaysInMonth(currentlySelectedDate.Year, currentlySelectedDate.Month);
        string[] daysArray = new string[37];

        int j = 1;
        for (int i = 0; j <= daysInMonth; i++)
        {
            if (i >= (int)currentlySelectedDate.DayOfWeek)
            {
                daysArray[i] = j.ToString();
                j++;
            }
        }

        LogicObject.GetVariable("MonthDays").Value = daysArray;
    }

    /// <summary>
    /// This method sets the input date based on the selected month and day, and then draws the calendar.
    /// </summary>
    /// <remarks>
    /// The method first retrieves the selected day and day number from the respective variables.
    /// It then calculates the date by adding the day number to the current date (adjusted for the correct day of the week).
    /// Finally, it updates the input date and calls the DrawCalendar() method to refresh the calendar display.
    /// </remarks>
    [ExportMethod]
    public void SetDateTime()
    {
        string selectedDay = selectedMonthYearVariable.Value;
        int dayNumber = selectedDayVariable.Value;

        if (selectedDay != "")
        {
            var currentlySelectedDate = GetSelectedDate();
            currentlySelectedDate = currentlySelectedDate.AddDays(dayNumber - 1);
            inputDateVariable.Value = currentlySelectedDate.Date;

        }

        DrawCalendar();
    }

    /// <summary>
    /// This method sets the current date to the system's current date and draws the calendar.
    /// </summary>
    /// <remarks>
    /// The method updates the <see cref="inputDateVariable.Value"/> with the current date and calls <see cref="DrawCalendar()"/> to render the calendar.
    /// </remarks>
    [ExportMethod]
    public void SetToday()
    {
        inputDateVariable.Value = DateTime.Now.Date;
        DrawCalendar();
    }

    /// <summary>
    /// This method draws the calendar based on the selected date and current date.
    /// It sets the day of the selected date, the current day, the selected month and year,
    /// and checks if the selected date is today.
    /// </summary>
    /// <remarks>
    /// The method also calls the SundayToSaturday() method to update the day labels.
    /// </remarks>
    private void DrawCalendar()
    {
        DateTime selectedDateTime = inputDateVariable.Value;
        selectedDayVariable.Value = selectedDateTime.Day.ToString();
        actualDayVariable.Value = DateTime.Now.Day.ToString();
        selectedMonthYearVariable.Value = selectedDateTime.Date.ToString("MMMM yyyy");
        selectedActMonthVariable.Value = true;

        isTodayVariable.Value = selectedDateTime.Month == DateTime.Now.Month &&
                                selectedDateTime.Year == DateTime.Now.Year;

        SundayToSaturday();
    }

    private DateTime GetSelectedDate() => DateTime.ParseExact(selectedMonthYearVariable.Value, "MMMM yyyy", CultureInfo.CurrentCulture);

    private IUAVariable selectedDayVariable;
    private IUAVariable actualDayVariable;
    private IUAVariable selectedMonthYearVariable;
    private IUAVariable selectedActMonthVariable;
    private IUAVariable isTodayVariable;
    private IUAVariable inputDateVariable;
}
