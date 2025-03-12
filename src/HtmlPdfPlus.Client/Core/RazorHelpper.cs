// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using RazorEngineCore;

namespace HtmlPdfPlus.Client.Core
{
    /// <summary>
    /// Razor Helpper
    /// </summary>
    internal sealed class RazorHelpper
    {
        private static readonly RazorEngine _engineRazor = new();
         /// <summary>
        /// Compiles and runs a Razor template with the specified model.
        /// </summary>
        /// <typeparam name="T">The type of the model.</typeparam>
        /// <param name="razorTemplate">The Razor template as a string.</param>
        /// <param name="model">The model to pass to the template.</param>
        /// <returns>The result of the template execution.</returns>
        public static string CompileTemplate<T>(string razorTemplate, T model)
        {
            var template = _engineRazor.Compile<RazorEngineTemplateBase<T>>(razorTemplate, builderAction: builder =>
            {
                builder.AddAssemblyReferenceByName("System");
                builder.AddAssemblyReferenceByName("System.Linq");
                builder.AddAssemblyReferenceByName("System.Collections");
            });
            return template.Run(instance =>
            {
                instance.Model = model;
            });
        }
    }
}
