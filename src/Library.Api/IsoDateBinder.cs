using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Library.Api;

// Dates are accepted as yyyy-MM-dd only. The default binder also reads "01/09/2025" month first,
// as 9 January, when most of the world means 1 September. That would silently shift the window.
public sealed class IsoDateBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var supplied = context.ValueProvider.GetValue(context.ModelName);

        if (supplied == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        context.ModelState.SetModelValue(context.ModelName, supplied);

        var value = supplied.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            context.Result = ModelBindingResult.Success(day);
        }
        else
        {
            context.ModelState.TryAddModelError(
                context.ModelName, $"The value '{value}' is not a date in the form yyyy-MM-dd.");
        }

        return Task.CompletedTask;
    }
}

public sealed class IsoDateBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context) =>
        context.Metadata.UnderlyingOrModelType == typeof(DateOnly) ? new IsoDateBinder() : null;
}
