SELECT
    LTRIM(RTRIM(CAST(m.ItemCode AS nvarchar(100)))) AS ProductCode,
    COALESCE(
        NULLIF(LTRIM(RTRIM(CAST(m.Barcode AS nvarchar(100)))), ''),
        NULLIF(LTRIM(RTRIM(CAST(st.Barcode AS nvarchar(100)))), '')
    ) AS Barcode,
    CAST(ISNULL(m.Description, N'') AS nvarchar(255)) AS ProductName,
    CAST(ISNULL(st.StockQty, 0) AS decimal(18,3)) AS StockQty,
    CAST(m.UnitPrice AS decimal(18,4)) AS Price
FROM dbo.LogicalMasterData AS m
LEFT JOIN (
    SELECT
        LTRIM(RTRIM(CAST(ItemCode AS nvarchar(100)))) AS ItemCode,
        SUM(CAST(ISNULL(Stock, 0) AS decimal(18,3))) AS StockQty,
        MAX(Barcode) AS Barcode
    FROM dbo.LogicalStock
    GROUP BY LTRIM(RTRIM(CAST(ItemCode AS nvarchar(100))))
) AS st
    ON st.ItemCode = LTRIM(RTRIM(CAST(m.ItemCode AS nvarchar(100))))
WHERE m.ItemCode IS NOT NULL
  AND LTRIM(RTRIM(CAST(m.ItemCode AS nvarchar(100)))) <> '';
