﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Python.Proxies.OS;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Python
{
    public class PythonEngineService
    {
        private readonly PythonProxyFactory _pythonProxyFactory = new PythonProxyFactory();
        private readonly StorageService _storageService;
        private readonly ILogger _logger;
        
        private ScriptEngine _scriptEngine;

        public PythonEngineService(StorageService storageService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<PythonEngineService>();
        }

        public void Start()
        {
            _logger.Log(LogLevel.Information, "Starting Python engine...");

            _scriptEngine = IronPython.Hosting.Python.CreateEngine();
            _scriptEngine.Runtime.IO.SetOutput(new PythonIOToLogStream(_logger), Encoding.UTF8);

            AddSearchPaths(_scriptEngine);

            var scriptHost = CreateScriptHost(_logger);
            scriptHost.Initialize("def test():\r\n    return 0");
            scriptHost.InvokeFunction("test");

            _logger.Log(LogLevel.Information, "Python engine started.");
        }

        public void RegisterSingletonProxy(IPythonProxy proxy)
        {
            if (proxy == null) throw new ArgumentNullException(nameof(proxy));

            _pythonProxyFactory.RegisterProxy(proxy);
        }

        public PythonScriptHost CreateScriptHost(params IPythonProxy[] customProxies)
        {
            return CreateScriptHost(null, customProxies);
        }

        public PythonScriptHost CreateScriptHost(ILogger logger, params IPythonProxy[] customProxies)
        {
            if (customProxies == null) throw new ArgumentNullException(nameof(customProxies));

            var scriptScope = _scriptEngine.CreateScope();

            var pythonProxies = _pythonProxyFactory.CreateProxies();
            pythonProxies.AddRange(customProxies);
            pythonProxies.Add(new LogPythonProxy(logger ?? _logger));
            pythonProxies.Add(new DebuggerPythonProxy());

            var wirehomePythonProxy = (IDictionary<string, object>)new ExpandoObject();

            foreach (var pythonProxy in pythonProxies)
            {
                // TODO: Remove this as soon as all entities are migrated.
                scriptScope.SetVariable(pythonProxy.ModuleName, pythonProxy);

                wirehomePythonProxy.Add(pythonProxy.ModuleName, pythonProxy);
            }

            scriptScope.SetVariable("wirehome", wirehomePythonProxy);
            return new PythonScriptHost(scriptScope);
        }

        private void AddSearchPaths(ScriptEngine scriptEngine)
        {
            var librariesPath = Path.Combine(_storageService.DataPath, "PythonLibraries");
            if (!Directory.Exists(librariesPath))
            {
                Directory.CreateDirectory(librariesPath);
            }

            var searchPaths = scriptEngine.GetSearchPaths();

            const string LinuxLibsPath = "/usr/lib/python2.7";
            if (Directory.Exists(LinuxLibsPath))
            {
                searchPaths.Add(LinuxLibsPath);
            }

            const string WindowsLibsPath = @"C:\Python27\Lib";
            if (Directory.Exists(WindowsLibsPath))
            {
                searchPaths.Add(WindowsLibsPath);
            }
            
            searchPaths.Add(librariesPath);
            scriptEngine.SetSearchPaths(searchPaths);
        }
    }
}
