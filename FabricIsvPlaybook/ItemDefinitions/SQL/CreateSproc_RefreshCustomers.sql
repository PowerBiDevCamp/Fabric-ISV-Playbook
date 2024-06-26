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