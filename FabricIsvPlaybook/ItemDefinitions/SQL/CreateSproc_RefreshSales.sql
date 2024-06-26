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