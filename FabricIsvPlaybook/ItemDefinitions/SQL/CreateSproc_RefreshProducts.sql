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