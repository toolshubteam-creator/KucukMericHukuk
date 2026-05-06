namespace KucukMericHukuk.Core.Common;

public static class ErrorCodes
{
    public static class Common
    {
        public const string NotFound = "Common.NotFound";
        public const string Validation = "Common.Validation";
        public const string Unauthorized = "Common.Unauthorized";
        public const string Conflict = "Common.Conflict";
        public const string Unexpected = "Common.Unexpected";
    }

    public static class Page
    {
        public const string NotFound = "Page.NotFound";
        public const string SlugExists = "Page.SlugExists";
        public const string PageKeyExists = "Page.PageKeyExists";
    }

    public static class Article
    {
        public const string NotFound = "Article.NotFound";
        public const string SlugExists = "Article.SlugExists";
        public const string InvalidStatus = "Article.InvalidStatus";
    }

    public static class Service
    {
        public const string NotFound = "Service.NotFound";
        public const string SlugExists = "Service.SlugExists";
        public const string AttorneyNotFound = "Service.AttorneyNotFound";
    }

    public static class Attorney
    {
        public const string NotFound = "Attorney.NotFound";
        public const string SlugExists = "Attorney.SlugExists";
        public const string UserAlreadyLinked = "Attorney.UserAlreadyLinked";
        public const string ServiceNotFound = "Attorney.ServiceNotFound";
    }

    public static class Category
    {
        public const string NotFound = "Category.NotFound";
        public const string SlugExists = "Category.SlugExists";
        public const string CircularParent = "Category.CircularParent";
        public const string HasChildren = "Category.HasChildren";
        public const string ParentNotFound = "Category.ParentNotFound";
        public const string ParentDeleted = "Category.ParentDeleted";
    }

    public static class Tag
    {
        public const string NotFound = "Tag.NotFound";
        public const string SlugExists = "Tag.SlugExists";
    }
}
