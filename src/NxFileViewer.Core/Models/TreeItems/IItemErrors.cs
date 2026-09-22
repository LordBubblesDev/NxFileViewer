using System.Collections.Generic;

namespace Emignatik.NxFileViewer.Models.TreeItems;

public interface IItemErrors : IEnumerable<ItemError>
{
    event ErrorsChangedHandler? ErrorsChanged;
    public int Count { get; }
    bool Add(ItemError error);
    int RemoveAllOfCategory(Category category);
}

public delegate void ErrorsChangedHandler(object sender, ErrorsChangedHandlerArgs args);

public class ErrorsChangedHandlerArgs(ItemError[] removedErrors, ItemError[] addedErrors)
{
    public ItemError[] RemovedErrors { get; } = removedErrors;
    public ItemError[] AddedErrors { get; } = addedErrors;
}

public class ItemError
{
    public string Message { get; init; } = string.Empty;

    public Category Category { get; init; }

    public override int GetHashCode()
    {
        return Message.GetHashCode() + Category.GetHashCode();
    }
}

public enum Category
{
    Loading,
    IntegrityCheck
}

public static class ItemErrorsExtension
{
    public static void Add(this IItemErrors itemErrors, Category category, string message)
    {
        itemErrors.Add(new ItemError { Category = category, Message = message });
    }
}