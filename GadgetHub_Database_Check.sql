-- =============================================
-- GadgetHub Database Table Check Script
-- This script checks the database structure and data
-- Run this in SQL Server Management Studio (SSMS)
-- =============================================

USE GadgetHubDB;
GO

PRINT '=============================================';
PRINT 'GadgetHub Database Table Check Report';
PRINT '=============================================';
PRINT '';

-- Check if database exists and is accessible
IF DB_ID('GadgetHubDB') IS NOT NULL
BEGIN
    PRINT '✅ Database Status: GadgetHubDB exists and is accessible';
    PRINT 'Database Size: ' + CAST(CAST(DB_SIZE('GadgetHubDB') AS DECIMAL(10,2)) AS VARCHAR(20)) + ' MB';
    PRINT '';
END
ELSE
BEGIN
    PRINT '❌ ERROR: Database GadgetHubDB does not exist or is not accessible';
    RETURN;
END

-- =============================================
-- 1. CHECK TABLE STRUCTURE
-- =============================================
PRINT '1. TABLE STRUCTURE CHECK';
PRINT '========================';

-- Check if all required tables exist
DECLARE @RequiredTables TABLE (TableName NVARCHAR(100));
INSERT INTO @RequiredTables VALUES 
    ('Customers'), ('Distributors'), ('Products'), ('Cart'), 
    ('Quotations'), ('QuotationItems'), ('DistributorResponses'), 
    ('Orders'), ('OrderItems'), ('Notifications');

DECLARE @MissingTables NVARCHAR(MAX) = '';
SELECT @MissingTables = @MissingTables + TableName + ', ' 
FROM @RequiredTables 
WHERE NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = TableName);

IF @MissingTables = ''
BEGIN
    PRINT '✅ All required tables exist';
END
ELSE
BEGIN
    PRINT '❌ Missing tables: ' + LEFT(@MissingTables, LEN(@MissingTables) - 1);
END

-- Check table row counts
PRINT '';
PRINT 'Table Row Counts:';
PRINT '-----------------';

SELECT 
    'Customers' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Customers
UNION ALL
SELECT 
    'Distributors' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Distributors
UNION ALL
SELECT 
    'Products' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Products
UNION ALL
SELECT 
    'Cart' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Cart
UNION ALL
SELECT 
    'Quotations' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Quotations
UNION ALL
SELECT 
    'QuotationItems' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM QuotationItems
UNION ALL
SELECT 
    'DistributorResponses' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM DistributorResponses
UNION ALL
SELECT 
    'Orders' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Orders
UNION ALL
SELECT 
    'OrderItems' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM OrderItems
UNION ALL
SELECT 
    'Notifications' AS TableName,
    COUNT(*) AS RowCount,
    CASE WHEN COUNT(*) > 0 THEN '✅ Has Data' ELSE '⚠️ Empty' END AS Status
FROM Notifications;

-- =============================================
-- 2. CHECK FOREIGN KEY CONSTRAINTS
-- =============================================
PRINT '';
PRINT '2. FOREIGN KEY CONSTRAINTS CHECK';
PRINT '================================';

SELECT 
    fk.name AS FK_Name,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn,
    '✅ Valid' AS Status
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id IN (
    OBJECT_ID('Cart'), OBJECT_ID('Quotations'), OBJECT_ID('QuotationItems'),
    OBJECT_ID('DistributorResponses'), OBJECT_ID('Orders'), OBJECT_ID('OrderItems'),
    OBJECT_ID('Notifications')
)
ORDER BY TableName, ColumnName;

-- =============================================
-- 3. CHECK INDEXES
-- =============================================
PRINT '';
PRINT '3. INDEXES CHECK';
PRINT '================';

SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    CASE WHEN i.is_unique = 1 THEN 'Unique' ELSE 'Non-Unique' END AS Uniqueness,
    '✅ Active' AS Status
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Cart', 'Quotations', 'DistributorResponses', 'Orders', 'Notifications')
AND i.name IS NOT NULL
ORDER BY t.name, i.name;

-- =============================================
-- 4. CHECK SAMPLE DATA
-- =============================================
PRINT '';
PRINT '4. SAMPLE DATA CHECK';
PRINT '====================';

-- Check Customers
PRINT 'Customers:';
IF EXISTS (SELECT 1 FROM Customers)
BEGIN
    SELECT TOP 3 CustomerID, Name, Email, Phone, CreatedDate FROM Customers ORDER BY CustomerID;
END
ELSE
BEGIN
    PRINT '⚠️ No customers found';
END

PRINT '';

-- Check Distributors
PRINT 'Distributors:';
IF EXISTS (SELECT 1 FROM Distributors)
BEGIN
    SELECT DistributorID, Name, Email, Phone, CreatedDate FROM Distributors ORDER BY DistributorID;
END
ELSE
BEGIN
    PRINT '⚠️ No distributors found';
END

PRINT '';

-- Check Products
PRINT 'Products:';
IF EXISTS (SELECT 1 FROM Products)
BEGIN
    SELECT TOP 5 ProductID, Name, Category, CreatedDate FROM Products ORDER BY ProductID;
END
ELSE
BEGIN
    PRINT '⚠️ No products found';
END

-- =============================================
-- 5. CHECK DATA INTEGRITY
-- =============================================
PRINT '';
PRINT '5. DATA INTEGRITY CHECK';
PRINT '=======================';

-- Check for orphaned records
DECLARE @OrphanedRecords TABLE (TableName NVARCHAR(100), Issue NVARCHAR(200), Count INT);

-- Check Cart for orphaned CustomerID references
INSERT INTO @OrphanedRecords
SELECT 'Cart', 'Orphaned CustomerID references', COUNT(*)
FROM Cart c
LEFT JOIN Customers cust ON c.CustomerID = cust.CustomerID
WHERE cust.CustomerID IS NULL;

-- Check Cart for orphaned ProductID references
INSERT INTO @OrphanedRecords
SELECT 'Cart', 'Orphaned ProductID references', COUNT(*)
FROM Cart c
LEFT JOIN Products p ON c.ProductID = p.ProductID
WHERE p.ProductID IS NULL;

-- Check Quotations for orphaned CustomerID references
INSERT INTO @OrphanedRecords
SELECT 'Quotations', 'Orphaned CustomerID references', COUNT(*)
FROM Quotations q
LEFT JOIN Customers cust ON q.CustomerID = cust.CustomerID
WHERE cust.CustomerID IS NULL;

-- Check QuotationItems for orphaned references
INSERT INTO @OrphanedRecords
SELECT 'QuotationItems', 'Orphaned QuotationID references', COUNT(*)
FROM QuotationItems qi
LEFT JOIN Quotations q ON qi.QuotationID = q.QuotationID
WHERE q.QuotationID IS NULL;

INSERT INTO @OrphanedRecords
SELECT 'QuotationItems', 'Orphaned ProductID references', COUNT(*)
FROM QuotationItems qi
LEFT JOIN Products p ON qi.ProductID = p.ProductID
WHERE p.ProductID IS NULL;

-- Display orphaned records summary
SELECT 
    TableName,
    Issue,
    Count,
    CASE WHEN Count = 0 THEN '✅ OK' ELSE '❌ ISSUE' END AS Status
FROM @OrphanedRecords
ORDER BY TableName, Issue;

-- =============================================
-- 6. CHECK DATABASE SIZE AND GROWTH
-- =============================================
PRINT '';
PRINT '6. DATABASE SIZE AND GROWTH';
PRINT '============================';

SELECT 
    DB_NAME() AS DatabaseName,
    CAST(CAST(DB_SIZE(DB_NAME()) AS DECIMAL(10,2)) AS VARCHAR(20)) AS DatabaseSizeMB,
    CAST(CAST(DB_SIZE(DB_NAME()) AS DECIMAL(10,2)) / 1024 AS VARCHAR(20)) AS DatabaseSizeGB,
    GETDATE() AS CheckDate;

-- =============================================
-- 7. RECOMMENDATIONS
-- =============================================
PRINT '';
PRINT '7. RECOMMENDATIONS';
PRINT '==================';

-- Check if database needs maintenance
IF EXISTS (SELECT 1 FROM @OrphanedRecords WHERE Count > 0)
BEGIN
    PRINT '❌ ACTION REQUIRED: Found orphaned records. Consider cleaning up data.';
END
ELSE
BEGIN
    PRINT '✅ No orphaned records found. Database integrity is good.';
END

-- Check if tables have data
IF NOT EXISTS (SELECT 1 FROM Customers) OR NOT EXISTS (SELECT 1 FROM Distributors) OR NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    PRINT '⚠️ WARNING: Some core tables are empty. Consider running the initial data insertion script.';
END
ELSE
BEGIN
    PRINT '✅ Core tables have data. Database is ready for use.';
END

-- Check database size
DECLARE @DbSizeMB DECIMAL(10,2) = CAST(DB_SIZE(DB_NAME()) AS DECIMAL(10,2));
IF @DbSizeMB > 1000
BEGIN
    PRINT '⚠️ WARNING: Database size is over 1GB. Consider archiving old data.';
END
ELSE
BEGIN
    PRINT '✅ Database size is reasonable.';
END

PRINT '';
PRINT '=============================================';
PRINT 'Database Check Complete!';
PRINT '=============================================';
PRINT '';

-- =============================================
-- 8. QUICK FIXES (if needed)
-- =============================================
PRINT '8. QUICK FIXES (if needed)';
PRINT '==========================';

-- If no customers exist, provide sample data
IF NOT EXISTS (SELECT 1 FROM Customers)
BEGIN
    PRINT 'To add sample customers, run:';
    PRINT 'INSERT INTO Customers (Name, Email, Password, Phone, Address) VALUES';
    PRINT '(''John Doe'', ''john@example.com'', ''password123'', ''123-456-7890'', ''123 Main St'')';
    PRINT '';
END

-- If no products exist, provide sample data
IF NOT EXISTS (SELECT 1 FROM Products)
BEGIN
    PRINT 'To add sample products, run:';
    PRINT 'INSERT INTO Products (Name, Description, Category) VALUES';
    PRINT '(''Sample Product'', ''Sample Description'', ''Sample Category'')';
    PRINT '';
END

-- Check if database is ready for registration
IF EXISTS (SELECT 1 FROM Customers) AND EXISTS (SELECT 1 FROM Distributors) AND EXISTS (SELECT 1 FROM Products)
BEGIN
    PRINT '✅ Database is ready for user registration and login!';
    PRINT 'You can now test the registration functionality.';
END
ELSE
BEGIN
    PRINT '⚠️ Database needs sample data before testing registration.';
END
