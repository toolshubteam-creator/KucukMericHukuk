namespace KucukMericHukuk.Core.Exceptions;

public class NotFoundException : Exception
{
    public string EntityName { get; }
    public object? Key { get; }

    public NotFoundException(string entityName, object? key = null)
        : base($"'{entityName}' bulunamadı{(key is null ? "." : $": {key}")}")
    {
        EntityName = entityName;
        Key = key;
    }
}
