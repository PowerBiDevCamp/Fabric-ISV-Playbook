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

GO

CREATE PROCEDURE [dbo].[refresh_products]
AS
BEGIN
  INSERT INTO products(ProductId, Product, Category)
  SELECT 
    ProductId, 
    Product, 
    Category 
  FROM staging.dbo.products
END

GO

CREATE PROCEDURE [dbo].[refresh_customers]
AS
BEGIN
  INSERT INTO customers(CustomerId, Country, City, DOB, Customer, Age) 
  SELECT 
    CustomerId, 
    Country, 
    City, 
    DOB,
    Customer = CONCAT(FirstName, ' ', LastName),
    Age = YEAR(CURRENT_TIMESTAMP) - YEAR(DOB)
  FROM staging.dbo.customers
END

GO

CREATE PROCEDURE [dbo].[refresh_sales]
AS
BEGIN
  INSERT INTO sales([Date], DateKey, CustomerId, ProductId, Quantity, Sales) 
  SELECT 
    [Date], 
    DateKey = (YEAR([Date]) * 10000) + (MONTH([Date]) * 100) + DAY([Date]),
    CustomerId,
    ProductId, 
    Quantity, 
    Sales = SalesAmount
  FROM staging.dbo.invoices 
  INNER JOIN staging.dbo.invoice_details 
  ON staging.dbo.invoices.InvoiceId = staging.dbo.invoice_details.InvoiceId
END

GO

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