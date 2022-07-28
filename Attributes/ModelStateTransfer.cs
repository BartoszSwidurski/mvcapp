using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MvcApp.Extensions;

namespace MvcApp.Attributes
{
    public abstract class ModelStateTempDataTransfer : ActionFilterAttribute
    {
        protected static readonly string ModelEntriesKey = typeof(ModelStateTempDataTransfer).FullName;

        protected internal class Entry
        {
            public object RawValue { get; set; }
            public string AttemptedValue { get; set; }
            public ModelValidationState State { get; set; }
            public IEnumerable<string> Errors { get; set; }

            public Entry()
            {
            }

            public Entry(ModelStateEntry entry)
            {
                RawValue = entry.RawValue;
                AttemptedValue = entry.AttemptedValue;
                State = entry.ValidationState;
                Errors = entry.Errors?.Select(x => x.ErrorMessage) ?? Enumerable.Empty<string>();
            }
        }
    }

    public class ExportModelStateToTempData : ModelStateTempDataTransfer
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var _factory = filterContext.HttpContext.RequestServices.GetService<ITempDataDictionaryFactory>();

            var tempData = _factory.GetTempData(filterContext.HttpContext);
            if (filterContext.ModelState.IsValid)
                return;
            if (IsInvalidResult(filterContext))
                return;

            var values = filterContext.ModelState.ToList()
                .Select(item => new KeyValuePair<string, Entry>(item.Key, new Entry(item.Value)))
                .ToDictionary(x => x.Key, x => x.Value);
            tempData[ModelEntriesKey] = values.ToJson();

            base.OnActionExecuted(filterContext);
        }

        private static bool IsInvalidResult(ActionExecutedContext context)
            => !(context.Result is RedirectToActionResult) &&
               !(context.Result is RedirectResult) &&
               !(context.Result is RedirectToRouteResult);
    }

    public class ImportModelStateFromTempData : ModelStateTempDataTransfer
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var _factory = filterContext.HttpContext.RequestServices.GetService<ITempDataDictionaryFactory >();

            var tempData = _factory.GetTempData(filterContext.HttpContext);
            if (ModelEntriesKey != null)
            {
                if (!tempData.ContainsKey(ModelEntriesKey))
                    return;
            }
            else
            {
                return;
            }


            var modelEntries = tempData[ModelEntriesKey].ToString().FromJson<Dictionary<string, Entry>>();
            if (modelEntries == null)
                return;

            foreach (var modelEntry in modelEntries)
            {
                filterContext.ModelState.AddModelError(modelEntry.Key, "Cannot instert value: " + modelEntry.Value.AttemptedValue);

                foreach (var error in modelEntry.Value.Errors)
                {
                    filterContext.ModelState.AddModelError(modelEntry.Key, error);
                }
            }

            tempData.Remove(ModelEntriesKey);
            base.OnActionExecuted(filterContext);
        }
    }
}
