-- GadgetHub Database Creation Script
-- Run this script in SQL Server Management Studio (SSMS)

CREATE DATABASE GadgetHubDB;
GO

USE GadgetHubDB;
GO

-- 1. Customers Table (with email and password - no hashing as requested)
CREATE TABLE Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- 2. Distributors Table (with email and password - no hashing as requested)
CREATE TABLE Distributors (
    DistributorID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- 3. Products Table
CREATE TABLE Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    ImageURL NVARCHAR(255),
    Category NVARCHAR(50),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- 4. Cart Table (individual carts for each customer)
CREATE TABLE Cart (
    CartID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- 5. Quotations Table (created when customer checks out)
CREATE TABLE Quotations (
    QuotationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Completed, Cancelled
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);

-- 6. Quotation Items Table (products in each quotation)
CREATE TABLE QuotationItems (
    QuotationItemID INT IDENTITY(1,1) PRIMARY KEY,
    QuotationID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    FOREIGN KEY (QuotationID) REFERENCES Quotations(QuotationID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- 7. Distributor Responses Table (distributor quotes for each quotation)
CREATE TABLE DistributorResponses (
    ResponseID INT IDENTITY(1,1) PRIMARY KEY,
    QuotationID INT NOT NULL,
    DistributorID INT NOT NULL,
    ProductID INT NOT NULL,
    PricePerUnit DECIMAL(10,2),
    AvailableQuantity INT,
    DeliveryDays INT,
    IsSubmitted BIT DEFAULT 0,
    SubmittedDate DATETIME,
    FOREIGN KEY (QuotationID) REFERENCES Quotations(QuotationID),
    FOREIGN KEY (DistributorID) REFERENCES Distributors(DistributorID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- 8. Orders Table (final orders placed with selected distributors)
CREATE TABLE Orders (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    QuotationID INT NOT NULL,
    CustomerID INT NOT NULL,
    SelectedDistributorID INT NOT NULL,
    TotalAmount DECIMAL(10,2),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Confirmed, Cancelled, Delivered
    EstimatedDeliveryDate DATETIME,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ConfirmedDate DATETIME,
    FOREIGN KEY (QuotationID) REFERENCES Quotations(QuotationID),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (SelectedDistributorID) REFERENCES Distributors(DistributorID)
);

-- 9. Order Items Table (products in each order)
CREATE TABLE OrderItems (
    OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    PricePerUnit DECIMAL(10,2),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- 10. Notifications Table (for customer notifications)
CREATE TABLE Notifications (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    OrderID INT,
    Message NVARCHAR(500) NOT NULL,
    IsRead BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID),
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
);

-- Insert the 3 Distributors (as requested - only 3 distributors)
INSERT INTO Distributors (Name, Email, Password, Phone, Address) VALUES
('TechWorld', 'admin@techworld.com', 'techworld123', '123-456-7890', '123 Tech Street, Tech City'),
('ElectroCom', 'admin@electrocom.com', 'electrocom123', '123-456-7891', '456 Electro Avenue, Electro City'),
('Gadget Central', 'admin@gadgetcentral.com', 'gadgetcentral123', '123-456-7892', '789 Gadget Boulevard, Gadget City');

-- Insert Sample Products
INSERT INTO Products (Name, Description, Category) VALUES
('iPhone 15 Pro', 'Latest iPhone with advanced features', 'Smartphones'),
('Samsung Galaxy S24', 'Premium Android smartphone', 'Smartphones'),
('MacBook Pro M3', 'High-performance laptop for professionals', 'Laptops'),
('Dell XPS 13', 'Ultra-portable Windows laptop', 'Laptops'),
('iPad Air', 'Versatile tablet for work and play', 'Tablets'),
('Surface Pro 9', 'Microsoft 2-in-1 tablet', 'Tablets'),
('AirPods Pro', 'Wireless earbuds with noise cancellation', 'Audio'),
('Sony WH-1000XM5', 'Premium noise-cancelling headphones', 'Audio'),
('Apple Watch Series 9', 'Advanced smartwatch', 'Wearables'),
('Samsung Galaxy Watch 6', 'Feature-rich Android smartwatch', 'Wearables');

-- Create indexes for better performance
CREATE INDEX IX_Cart_CustomerID ON Cart(CustomerID);
CREATE INDEX IX_Quotations_CustomerID ON Quotations(CustomerID);
CREATE INDEX IX_DistributorResponses_QuotationID ON DistributorResponses(QuotationID);
CREATE INDEX IX_Orders_CustomerID ON Orders(CustomerID);
CREATE INDEX IX_Notifications_CustomerID ON Notifications(CustomerID);

PRINT 'GadgetHub Database created successfully!';