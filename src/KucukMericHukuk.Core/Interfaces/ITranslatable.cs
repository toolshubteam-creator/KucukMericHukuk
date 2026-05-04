namespace KucukMericHukuk.Core.Interfaces;

public interface ITranslatable<TTranslation> where TTranslation : ITranslation
{
    ICollection<TTranslation> Translations { get; set; }
}

public interface ITranslation
{
    int Id { get; set; }
    string LanguageCode { get; set; }
}
