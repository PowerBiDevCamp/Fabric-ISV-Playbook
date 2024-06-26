CREATE PROCEDURE [dbo].[refresh_calendar]
AS
BEGIN
  -- create calendar table
  SET datefirst 1;

  DECLARE @MinDate date = (SELECT MIN([Date]) FROM sales)
  DECLARE @MaxDate date =  (SELECT MAX([Date]) FROM sales)

  DECLARE @StartDate date = (SELECT DATEFROMPARTS(YEAR(@MinDate), 1, 1))
  DECLARE @EndDate date = (SELECT DATEFROMPARTS(YEAR(@MaxDate), 12, 31))

  DECLARE @DayCount int = DATEDIFF(DAY, @StartDate, @EndDate) + 1

  INSERT INTO calendar([Date], DateKey,  Year, Quarter, Month, Day, MonthInYear, MonthInYearSort, DayOfWeek, DayOfWeekSort) 
  SELECT
    [Date] = DATEADD(day, value, @StartDate),
    DateKey =  (YEAR(DATEADD(day, value, @StartDate)) * 10000) + (MONTH(DATEADD(day, value, @StartDate)) * 100) + DAY(DATEADD(day, value, @StartDate)),
    Year = YEAR(DATEADD(day, value, @StartDate)),
    Quarter = CONCAT(YEAR(DATEADD(day, value, @StartDate)), '-Q',  DATEPART(quarter, DATEADD(day, value, @StartDate) )),
    Month = FORMAT ( DATEADD(day, value, @StartDate), 'yyyy-MM' ),
    Day = DAY(DATEADD(day, value, @StartDate)), 
    MonthInYear = FORMAT ( DATEADD(day, value, @StartDate), 'MMMM' ),
    MonthInYearSort = MONTH(DATEADD(day, value, @StartDate)),
    DayOfWeek = DATENAME(WEEKDAY, DATEADD(day, value, @StartDate)),
    DayOfWeekSort = DATEPART(weekday, DATEADD(day, value, @StartDate))
  FROM GENERATE_SERIES(0, @DayCount, 1)

END