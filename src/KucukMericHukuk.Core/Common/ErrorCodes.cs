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
        public const string CategoryNotFound = "Article.CategoryNotFound";
        public const string TranslationRequired = "Article.TranslationRequired";
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

    public static class Media
    {
        public const string NotFound = "Media.NotFound";
        public const string FileTooLarge = "Media.FileTooLarge";
        public const string UnsupportedMediaType = "Media.UnsupportedMediaType";
        public const string ProcessingFailed = "Media.ProcessingFailed";
        public const string StorageFailed = "Media.StorageFailed";
    }

    public static class Faq
    {
        public const string NotFound = "Faq.NotFound";
        public const string TranslationRequired = "Faq.TranslationRequired";
        public const string ReorderInvalid = "Faq.ReorderInvalid";
    }

    public static class Testimonial
    {
        public const string NotFound = "Testimonial.NotFound";
        public const string TranslationRequired = "Testimonial.TranslationRequired";
    }

    public static class ContactMessage
    {
        public const string NotFound = "ContactMessage.NotFound";
        public const string ReplyBodyRequired = "ContactMessage.ReplyBodyRequired";
    }

    public static class Appointment
    {
        public const string NotFound = "Appointment.NotFound";
    }

    public static class Subscriber
    {
        public const string NotFound = "Subscriber.NotFound";
        public const string AlreadySubscribed = "Subscriber.AlreadySubscribed";
        public const string InvalidToken = "Subscriber.InvalidToken";
    }

    public static class Newsletter
    {
        public const string ArticleNotFound = "Newsletter.ArticleNotFound";
        public const string ArticleNotPublished = "Newsletter.ArticleNotPublished";
        public const string AlreadySent = "Newsletter.AlreadySent";
        public const string ActiveJobExists = "Newsletter.ActiveJobExists";
        public const string JobNotFound = "Newsletter.JobNotFound";
        public const string JobNotPending = "Newsletter.JobNotPending";
        public const string JobAlreadyProcessing = "Newsletter.JobAlreadyProcessing";
        public const string NoActiveSubscribers = "Newsletter.NoActiveSubscribers";
    }

    public static class AuditLog
    {
        public const string NotFound = "AuditLog.NotFound";
    }

    public static class SiteSetting
    {
        public const string NotFound = "SiteSetting.NotFound";
        public const string ValidationFailed = "SiteSetting.ValidationFailed";
        public const string GroupNotFound = "SiteSetting.GroupNotFound";
    }

    public static class User
    {
        public const string NotFound = "User.NotFound";
        public const string EmailAlreadyExists = "User.EmailAlreadyExists";
        public const string UserNameAlreadyExists = "User.UserNameAlreadyExists";
        public const string CannotDeactivateSelf = "User.CannotDeactivateSelf";
        public const string CannotDeactivateLastAdmin = "User.CannotDeactivateLastAdmin";
        public const string CannotChangeOwnRole = "User.CannotChangeOwnRole";
        public const string InvalidRole = "User.InvalidRole";
        public const string IdentityError = "User.IdentityError";
    }
}
