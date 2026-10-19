namespace Samagra.Functions.Import;

// CSV header: Name,Description,Price,Stock,CategoryName,ImageUrl
internal static class ProductCsvColumns
{
    public const string Name = "Name";
    public const string Description = "Description";
    public const string Price = "Price";
    public const string Stock = "Stock";
    public const string CategoryName = "CategoryName";
    public const string ImageUrl = "ImageUrl";

    public static readonly string[] Required = [Name, Price, Stock, CategoryName];
}
