using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace StoreApp.Web.Services;

public sealed class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.Metadata.UnderlyingOrModelType == typeof(decimal) ? new DecimalModelBinder() : null;
}

public sealed class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var result = ctx.ValueProvider.GetValue(ctx.ModelName);
        if (result == ValueProviderResult.None) return Task.CompletedTask;
        ctx.ModelState.SetModelValue(ctx.ModelName, result);

        var t = ctx.HttpContext.RequestServices.GetRequiredService<IStringLocalizer>();
        var text = result.FirstValue;

        if (string.IsNullOrWhiteSpace(text))
        {
            if (ctx.ModelMetadata.IsReferenceOrNullableType) ctx.Result = ModelBindingResult.Success(null);
            else ctx.ModelState.TryAddModelError(ctx.ModelName, t["Заполните поле"].Value);
        }
        else if (Num.TryParse(text, out var value))
            ctx.Result = ModelBindingResult.Success(value);
        else
            ctx.ModelState.TryAddModelError(ctx.ModelName, t["Некорректное число"].Value);

        return Task.CompletedTask;
    }
}