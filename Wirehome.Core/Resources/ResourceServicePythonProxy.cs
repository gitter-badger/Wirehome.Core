﻿#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Resources
{
    public class ResourceServicePythonProxy : IInjectedPythonProxy
    {
        private readonly ResourceService _resourceService;

        public ResourceServicePythonProxy(ResourceService resourceService)
        {
            _resourceService = resourceService ?? throw new ArgumentNullException(nameof(resourceService));
        }

        public string ModuleName { get; } = "resources";

        public bool register_value(string uid, string languageCode, string value)
        {
            return _resourceService.RegisterResource(uid, languageCode, value);
        }
        
        public string get_value(string uid, string defaultValue = "")
        {
            return _resourceService.GetResourceValue(uid, defaultValue);
        }

        public string get_language_value(string uid, string languageCode, string defaultValue = "")
        {
            return _resourceService.GetLanguageResourceValue(uid, languageCode, defaultValue);
        }

        public string get_formatted_value(string uid, PythonDictionary parameters, string defaultValue = "")
        {
            var value = _resourceService.GetResourceValue(uid, defaultValue);
            value = _resourceService.FormatValue(value, parameters);

            return value;
        }
    }
}