CREATE PROCEDURE [dbo].[create_all_tables]
AS
BEGIN

  DROP TABLE IF EXISTS [dbo].[products];
  CREATE TABLE [dbo].[products] (
    [ProductId] [int] NULL,
    [Product] [varchar](50) NULL,
    [Category] [varchar](50) NULL
  );

  DROP TABLE IF EXISTS [dbo].[customers];
  CREATE TABLE [dbo].[customers] (
    [CustomerId] [int] NULL,
    [Country] [varchar](50) NULL,
    [City] [varchar](50) NULL,
    [DOB] [date] NULL,
    [Customer] [varchar](50) NULL,
    [Age] [int] NULL
  );

  DROP TABLE IF EXISTS [dbo].[sales];
  CREATE TABLE [dbo].[sales] (
    [Date] [date] NULL,
    [DateKey] [int] NULL,
    [CustomerId] [int] NULL,
    [ProductId] [int] NULL,
    [Sales] [decimal](18, 2) NULL,
    [Quantity] [int] NULL
  );

  DROP TABLE IF EXISTS [dbo].[calendar];
  CREATE TABLE [dbo].[calendar] (
    [Date] [date] NULL,
    [DateKey] [int] NULL,
    [Year] [int] NULL,
    [Quarter] [varchar](50) NULL,
    [Month] [varchar](50) NULL,
    [Day] [int] NULL,
    [MonthInYear] [varchar](50) NULL,
    [MonthInYearSort] [int] NULL,
    [DayOfWeek] [varchar](50) NULL,
    [DayOfWeekSort] [int] NULL
  );

END