 -- Orders Table
CREATE TABLE Orders (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId      NVARCHAR(50)     NOT NULL,
    TotalAmount DECIMAL(18,2)    NOT NULL,
    Status      NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
    CreatedAt   DATETIME2        DEFAULT GETUTCDATE()
);

-- OrderItems Table — Order mein kaun kaun si products hain
CREATE TABLE OrderItems (
    Id          UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderId     UNIQUEIDENTIFIER NOT NULL,
    ProductId   UNIQUEIDENTIFIER NOT NULL,
    Quantity    INT              NOT NULL,
    UnitPrice   DECIMAL(18,2)    NOT NULL,

    FOREIGN KEY (OrderId)   REFERENCES Orders(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- Payments Table
CREATE TABLE Payments (
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderId         UNIQUEIDENTIFIER NOT NULL,
    Amount          DECIMAL(18,2)    NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
    PaymentMethod   NVARCHAR(20)     NOT NULL DEFAULT 'UPI',
    CreatedAt       DATETIME2        DEFAULT GETUTCDATE(),

    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';