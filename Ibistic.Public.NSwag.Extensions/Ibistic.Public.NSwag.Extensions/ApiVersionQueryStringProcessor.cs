using Namotion.Reflection;
using NSwag.Generation.Processors.Contexts;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NSwagProcessors = NSwag.Generation.Processors;

namespace Ibistic.Public.NSwag.Extensions
{
    public class ApiVersionQueryStringProcessor : NSwagProcessors.IOperationProcessor
    {
        /// <summary>Processes an action controller trying to add its api version to the action query string path.</summary>
        /// <param name="context">The processor context.</param>
        /// <returns>true if the action should be added to the OpenApi specification.</returns>
        public bool Process(OperationProcessorContext context)
        {
            var nswagApiVersionProcessor = context.Settings.OperationProcessors.TryGet<NSwagProcessors.ApiVersionProcessor>();
            string[] filteredVersions = nswagApiVersionProcessor.IncludedVersions;

            string higherActionApiVersion = GetHigherActionApiVersion(context);

            // If no ApiVersion attribute found for the action and its controller...
            if (String.IsNullOrEmpty(higherActionApiVersion))
            {
                // Include action in the OpenApi specification if the document is not being created with filtered versions.
                return filteredVersions == null || filteredVersions.Length == 0;
            }

            // If no filter by versions, include it
            if (filteredVersions == null || filteredVersions.Length == 0)
            {
                IncludeApiVersionInQueryString(context, higherActionApiVersion);
                return true;
            }

            // Add action to specification if the action version is included in the list of filtered versions
            if (filteredVersions.Contains(higherActionApiVersion))
            {
                IncludeApiVersionInQueryString(context, higherActionApiVersion);
                return true;
            }

            return false;
        }

        private string GetHigherActionApiVersion(OperationProcessorContext context)
        {
            // Try get highest api version defined at action level
            System.Attribute[] actionApiVersionAttributes = context.MethodInfo.GetCustomAttributes()
                .GetAssignableToTypeName("ApiVersionAttribute", TypeNameStyle.Name)
                .Where(a => a.HasProperty("Versions"))
                .ToArray();

            if (actionApiVersionAttributes.Length > 0)
            {
                return actionApiVersionAttributes.SelectMany((dynamic a) => ((IEnumerable) a.Versions).OfType<object>().Select(v => v.ToString()))
                    .First();
            }

            // Try get highest api version defined at controller level
            return context.ControllerType.GetTypeInfo().GetCustomAttributes(true)
                .GetAssignableToTypeName("ApiVersionAttribute", TypeNameStyle.Name)
                .Where(a => a.HasProperty("Versions"))
                .SelectMany((dynamic a) => ((IEnumerable)a.Versions).OfType<object>().Select(v => v.ToString()))
                .FirstOrDefault();
        }

        private void IncludeApiVersionInQueryString(OperationProcessorContext context, string version)
        {
            var operationDescription = context.OperationDescription;
            operationDescription.Path = $"{operationDescription.Path}?api-version={version}";
        }
    }
}
