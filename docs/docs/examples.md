---
layout: default
title: Examples
---

# ProxyR Examples

This guide provides practical examples of common use cases when working with ProxyR.

## Basic CRUD Operations

### 1. List View

```sql
CREATE VIEW ProxyR.Api_Products_View AS
SELECT
    Id,
    Name,
    Description,
    Price,
    CategoryId,
    IsActive
FROM dbo.Product;

-- Access via: GET /products
```

### 2. Filtered Grid

```sql
CREATE FUNCTION ProxyR.Api_Products_Grid
(
    @SearchTerm NVARCHAR(50) = NULL,
    @CategoryId INT = NULL,
    @MinPrice DECIMAL(18,2) = NULL,
    @MaxPrice DECIMAL(18,2) = NULL,
    @IsActive BIT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        c.Name AS CategoryName,
        p.IsActive
    FROM dbo.Product p
    INNER JOIN dbo.Category c ON p.CategoryId = c.Id
    WHERE
        (@SearchTerm IS NULL OR
         p.Name LIKE '%' + @SearchTerm + '%' OR
         p.Description LIKE '%' + @SearchTerm + '%')
        AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
        AND (@MinPrice IS NULL OR p.Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR p.Price <= @MaxPrice)
        AND (@IsActive IS NULL OR p.IsActive = @IsActive)
);

-- Access via: GET /products/grid?searchTerm=laptop&minPrice=500&isActive=true
```

## Advanced Queries

### 1. Aggregated Data

```sql
CREATE FUNCTION ProxyR.Api_Sales_Summary
(
    @StartDate DATE,
    @EndDate DATE,
    @GroupBy NVARCHAR(20) = 'day' -- 'day', 'week', 'month'
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        CASE @GroupBy
            WHEN 'day' THEN CAST(OrderDate AS DATE)
            WHEN 'week' THEN DATEADD(DAY, 1-DATEPART(WEEKDAY, OrderDate), CAST(OrderDate AS DATE))
            WHEN 'month' THEN DATEFROMPARTS(YEAR(OrderDate), MONTH(OrderDate), 1)
        END AS Period,
        COUNT(*) AS OrderCount,
        SUM(TotalAmount) AS TotalSales,
        AVG(TotalAmount) AS AverageOrderValue
    FROM dbo.Order
    WHERE
        OrderDate >= @StartDate AND
        OrderDate < DATEADD(DAY, 1, @EndDate)
    GROUP BY
        CASE @GroupBy
            WHEN 'day' THEN CAST(OrderDate AS DATE)
            WHEN 'week' THEN DATEADD(DAY, 1-DATEPART(WEEKDAY, OrderDate), CAST(OrderDate AS DATE))
            WHEN 'month' THEN DATEFROMPARTS(YEAR(OrderDate), MONTH(OrderDate), 1)
        END
);

-- Access via: GET /sales/summary?startDate=2024-01-01&endDate=2024-12-31&groupBy=month
```

### 2. Nested Data

```sql
CREATE FUNCTION ProxyR.Api_Orders_Details
(
    @OrderId INT = NULL,
    @CustomerId INT = NULL,
    @Status NVARCHAR(50) = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        o.Id AS OrderId,
        o.OrderDate,
        o.Status,
        o.TotalAmount,
        JSON_OBJECT(
            'id': c.Id,
            'name': c.Name,
            'email': c.Email
        ) AS Customer,
        (
            SELECT
                oi.ProductId,
                p.Name AS ProductName,
                oi.Quantity,
                oi.UnitPrice,
                oi.Quantity * oi.UnitPrice AS TotalPrice
            FROM dbo.OrderItem oi
            INNER JOIN dbo.Product p ON oi.ProductId = p.Id
            WHERE oi.OrderId = o.Id
            FOR JSON PATH
        ) AS Items
    FROM dbo.Order o
    INNER JOIN dbo.Customer c ON o.CustomerId = c.Id
    WHERE
        (@OrderId IS NULL OR o.Id = @OrderId)
        AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)
        AND (@Status IS NULL OR o.Status = @Status)
);

-- Access via: GET /orders/details?customerId=123&status=completed
```

## Multi-Table Operations

### 1. Dashboard Data

```sql
CREATE FUNCTION ProxyR.Api_Dashboard_Summary
(
    @UserId INT,
    @Period NVARCHAR(20) = 'today' -- 'today', 'week', 'month', 'year'
)
RETURNS TABLE
AS
RETURN
(
    WITH DateRange AS (
        SELECT
            CASE @Period
                WHEN 'today' THEN CAST(GETDATE() AS DATE)
                WHEN 'week' THEN DATEADD(WEEK, -1, GETDATE())
                WHEN 'month' THEN DATEADD(MONTH, -1, GETDATE())
                WHEN 'year' THEN DATEADD(YEAR, -1, GETDATE())
            END AS StartDate,
            GETDATE() AS EndDate
    )
    SELECT
        (SELECT COUNT(*) FROM dbo.Order o
         WHERE o.UserId = @UserId
         AND o.OrderDate BETWEEN d.StartDate AND d.EndDate) AS OrderCount,

        (SELECT SUM(TotalAmount) FROM dbo.Order o
         WHERE o.UserId = @UserId
         AND o.OrderDate BETWEEN d.StartDate AND d.EndDate) AS TotalSales,

        (SELECT COUNT(*) FROM dbo.Product p
         WHERE p.UserId = @UserId AND p.IsActive = 1) AS ActiveProducts,

        (SELECT COUNT(*) FROM dbo.Customer c
         WHERE c.AssignedUserId = @UserId) AS CustomerCount,

        (SELECT TOP 5
            JSON_OBJECT(
                'productId': p.Id,
                'name': p.Name,
                'totalSales': SUM(oi.Quantity)
            )
         FROM dbo.OrderItem oi
         INNER JOIN dbo.Product p ON oi.ProductId = p.Id
         INNER JOIN dbo.Order o ON oi.OrderId = o.Id
         WHERE o.UserId = @UserId
         AND o.OrderDate BETWEEN d.StartDate AND d.EndDate
         GROUP BY p.Id, p.Name
         ORDER BY SUM(oi.Quantity) DESC
         FOR JSON PATH) AS TopProducts
    FROM DateRange d
);

-- Access via: GET /dashboard/summary?userId=123&period=month
```

### 2. Search Across Tables

```sql
CREATE FUNCTION ProxyR.Api_Global_Search
(
    @SearchTerm NVARCHAR(100),
    @Types NVARCHAR(MAX) = NULL -- JSON array of types to search
)
RETURNS TABLE
AS
RETURN
(
    WITH SearchTypes AS (
        SELECT value AS Type
        FROM OPENJSON(@Types)
        WHERE @Types IS NOT NULL
        UNION ALL
        SELECT 'all' WHERE @Types IS NULL
    )
    SELECT
        'product' AS Type,
        CAST(Id AS NVARCHAR(50)) AS Id,
        Name AS Title,
        Description AS Description,
        'products/' + CAST(Id AS NVARCHAR(50)) AS Url
    FROM dbo.Product
    WHERE
        (EXISTS (SELECT 1 FROM SearchTypes WHERE Type IN ('all', 'product')))
        AND (
            Name LIKE '%' + @SearchTerm + '%' OR
            Description LIKE '%' + @SearchTerm + '%'
        )

    UNION ALL

    SELECT
        'customer' AS Type,
        CAST(Id AS NVARCHAR(50)) AS Id,
        Name AS Title,
        Email AS Description,
        'customers/' + CAST(Id AS NVARCHAR(50)) AS Url
    FROM dbo.Customer
    WHERE
        (EXISTS (SELECT 1 FROM SearchTypes WHERE Type IN ('all', 'customer')))
        AND (
            Name LIKE '%' + @SearchTerm + '%' OR
            Email LIKE '%' + @SearchTerm + '%'
        )

    UNION ALL

    SELECT
        'order' AS Type,
        CAST(o.Id AS NVARCHAR(50)) AS Id,
        'Order #' + CAST(o.Id AS NVARCHAR(50)) AS Title,
        c.Name + ' - ' + CAST(o.TotalAmount AS NVARCHAR(50)) AS Description,
        'orders/' + CAST(o.Id AS NVARCHAR(50)) AS Url
    FROM dbo.Order o
    INNER JOIN dbo.Customer c ON o.CustomerId = c.Id
    WHERE
        (EXISTS (SELECT 1 FROM SearchTypes WHERE Type IN ('all', 'order')))
        AND (
            CAST(o.Id AS NVARCHAR(50)) LIKE '%' + @SearchTerm + '%' OR
            c.Name LIKE '%' + @SearchTerm + '%'
        )
);

-- Access via: GET /global/search?searchTerm=laptop&types=["product","order"]
```

## Integration Examples

### 1. External API Integration

```sql
CREATE FUNCTION ProxyR.Api_Products_WithStock
(
    @CategoryId INT = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        i.Quantity AS StockQuantity,
        i.Location AS WarehouseLocation,
        CASE
            WHEN i.Quantity > 10 THEN 'In Stock'
            WHEN i.Quantity > 0 THEN 'Low Stock'
            ELSE 'Out of Stock'
        END AS StockStatus
    FROM dbo.Product p
    CROSS APPLY OPENJSON((
        SELECT TOP 1 Response
        FROM dbo.ExternalApiCache
        WHERE
            EndpointName = 'inventory'
            AND ResourceId = p.Id
            AND CachedAt >= DATEADD(MINUTE, -15, GETUTCDATE())
    )) WITH (
        Quantity INT '$.quantity',
        Location NVARCHAR(100) '$.location'
    ) AS i
    WHERE @CategoryId IS NULL OR p.CategoryId = @CategoryId
);

-- Access via: GET /products/withstock?categoryId=1
```

### 2. Report Generation

```sql
CREATE FUNCTION ProxyR.Api_Sales_Report
(
    @StartDate DATE,
    @EndDate DATE,
    @Format NVARCHAR(10) = 'json' -- 'json', 'csv'
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        o.OrderDate,
        c.Name AS CustomerName,
        p.Name AS ProductName,
        oi.Quantity,
        oi.UnitPrice,
        oi.Quantity * oi.UnitPrice AS TotalAmount,
        cat.Name AS Category,
        u.Username AS SalesRep
    FROM dbo.Order o
    INNER JOIN dbo.Customer c ON o.CustomerId = c.Id
    INNER JOIN dbo.OrderItem oi ON o.Id = oi.OrderId
    INNER JOIN dbo.Product p ON oi.ProductId = p.Id
    INNER JOIN dbo.Category cat ON p.CategoryId = cat.Id
    INNER JOIN dbo.User u ON o.UserId = u.Id
    WHERE
        o.OrderDate >= @StartDate
        AND o.OrderDate < DATEADD(DAY, 1, @EndDate)
);

-- Access via: GET /sales/report?startDate=2024-01-01&endDate=2024-01-31&format=csv
```

## Next Steps

- Learn about [Security Best Practices](./security.html)
- Explore [Configuration Options](./configuration.html)
- Check out [Query Parameters](./query-parameters.html)