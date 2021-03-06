﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Cloud.Services.Repository.Models;
using Wirehome.Core.Storage;

namespace Wirehome.Cloud.Services.Repository
{
    public class RepositoryService
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly string _rootPath;
        private readonly ILogger _logger;
        
        public RepositoryService(ILogger<RepositoryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (Debugger.IsAttached)
            {
                _rootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "Wirehome.Cloud", "Identities");
            }
            else
            {
                _rootPath = "D:/home/data/Wirehome.Cloud/Identities";
            }
        }

        public async Task<IdentityConfiguration> TryGetIdentityConfigurationAsync(string identityUid)
        {
            if (identityUid == null) throw new ArgumentNullException(nameof(identityUid));

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                return await TryReadIdentityConfigurationAsync(identityUid).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SetPasswordAsync(string identityUid, string newPassword)
        {
            if (identityUid == null) throw new ArgumentNullException(nameof(identityUid));
            if (newPassword == null) throw new ArgumentNullException(nameof(newPassword));

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                var identityConfiguration = await TryReadIdentityConfigurationAsync(identityUid).ConfigureAwait(false);

                var passwordHasher = new PasswordHasher<string>();
                identityConfiguration.PasswordHash = passwordHasher.HashPassword(string.Empty, newPassword);

                await WriteIdentityConfigurationAsync(identityUid, identityConfiguration).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<IdentityConfiguration> TryReadIdentityConfigurationAsync(string identityUid)
        {
            var filename = Path.Combine(_rootPath, identityUid, DefaultFilenames.Configuration);
            if (!File.Exists(filename))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filename, Encoding.UTF8).ConfigureAwait(false);
                var identityConfiguration = JsonConvert.DeserializeObject<IdentityConfiguration>(json);

                return identityConfiguration;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while loading file '{filename}'.");

                return null;
            }
        }

        private Task WriteIdentityConfigurationAsync(string identityUid, IdentityConfiguration identityConfiguration)
        {
            if (identityUid == null) throw new ArgumentNullException(nameof(identityUid));
            if (identityConfiguration == null) throw new ArgumentNullException(nameof(identityConfiguration));

            var filename = Path.Combine(_rootPath, identityUid);
            if (!Directory.Exists(filename))
            {
                Directory.CreateDirectory(filename);
            }

            filename = Path.Combine(filename, DefaultFilenames.Configuration);

            var json = JsonConvert.SerializeObject(identityConfiguration);
            return File.WriteAllTextAsync(filename, json, Encoding.UTF8);
        }
    }
}
