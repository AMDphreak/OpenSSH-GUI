#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Static;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public static class DirectoryCrawler
{
    private static ILogger _logger = null!;

    /// <summary>
    ///     Provides the logger context for the DirectoryCrawler class.
    /// </summary>
    /// <param name="logger">The logger instance to be used by the DirectoryCrawler class.</param>
    public static void ProvideContext(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Parses the SSH config file and extracts all IdentityFile paths.
    /// </summary>
    /// <returns>An enumerable collection of resolved IdentityFile paths.</returns>
    private static IEnumerable<string> GetIdentityFilePathsFromConfig()
    {
        var configPath = SshConfigFiles.Config.GetPathOfFile();
        if (!File.Exists(configPath))
        {
            yield break;
        }

        var paths = new List<string>();
        try
        {
            using var reader = new StreamReader(configPath);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmedLine = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                {
                    continue;
                }

                // Check if this is an IdentityFile line
                if (trimmedLine.StartsWith("IdentityFile", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var identityFilePath = parts[1];
                        var resolvedPath = ResolveIdentityFilePath(identityFilePath);
                        if (resolvedPath != null && File.Exists(resolvedPath))
                        {
                            paths.Add(resolvedPath);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing SSH config file at {path}", configPath);
        }

        foreach (var path in paths)
        {
            yield return path;
        }
    }

    /// <summary>
    ///     Resolves an IdentityFile path from SSH config, handling tildes, relative paths, and environment variables.
    /// </summary>
    /// <param name="path">The path from the IdentityFile directive.</param>
    /// <returns>The resolved absolute path, or null if the path cannot be resolved.</returns>
    private static string? ResolveIdentityFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        // Remove quotes if present
        path = path.Trim().Trim('"', '\'');

        // Handle tilde expansion first (~ or ~user)
        if (path.StartsWith("~/", StringComparison.Ordinal))
        {
            var baseSshPath = SshConfigFilesExtension.GetBaseSshPath();
            path = Path.Combine(baseSshPath, path.Substring(2));
        }
        else if (path == "~")
        {
            return null; // Just a tilde, not a valid path
        }
        else if (path.StartsWith("~", StringComparison.Ordinal) && path.Length > 1)
        {
            // Handle ~user format - treat as relative to base SSH path
            var baseSshPath = SshConfigFilesExtension.GetBaseSshPath();
            var slashIndex = path.IndexOf('/');
            if (slashIndex > 0)
            {
                path = Path.Combine(baseSshPath, path.Substring(slashIndex + 1));
            }
            else
            {
                return null; // Invalid format
            }
        }

        // Handle environment variables (expand after tilde processing)
        path = Environment.ExpandEnvironmentVariables(path);

        // If path is relative, make it relative to ~/.ssh
        if (!Path.IsPathRooted(path))
        {
            var baseSshPath = SshConfigFilesExtension.GetBaseSshPath();
            path = Path.Combine(baseSshPath, path);
        }

        // Normalize the path
        try
        {
            path = Path.GetFullPath(path);
            return path;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve IdentityFile path: {path}", path);
            return null;
        }
    }

    /// <summary>
    ///     Retrieves SSH keys from disk.
    /// </summary>
    /// <param name="convert">
    ///     Optional. Indicates whether to automatically convert PuTTY keys to OpenSSH format. Default is
    ///     false.
    /// </param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    private static IEnumerable<ISshKey> GetFromDisk(bool convert)
    {
        var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var baseSshPath = SshConfigFilesExtension.GetBaseSshPath();

        // First, get keys from the standard ~/.ssh directory
        // Prioritize public keys (.pub) and PuTTY keys (.ppk)
        foreach (var filePath in Directory
                     .EnumerateFiles(baseSshPath, "*", SearchOption.TopDirectoryOnly)
                     .Where(e => e.EndsWith(".pub", StringComparison.OrdinalIgnoreCase) || 
                                 e.EndsWith(".ppk", StringComparison.OrdinalIgnoreCase)))
        {
            if (processedPaths.Contains(filePath))
            {
                continue;
            }

            ISshKey? key = null;
            try
            {
                key = KeyFactory.FromPath(filePath);
                if (key is IPpkKey && convert) key = KeyFactory.ConvertToOppositeFormat(key, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading key from {path}", filePath);
            }

            if (key is null) continue;
            processedPaths.Add(filePath);
            
            // Mark the corresponding keypair file as processed to prevent duplicate registration
            if (key is ISshPublicKey)
            {
                // If this is a public key, mark the corresponding private key as processed
                // The public key already references its private key via the PrivateKey property
                var privateKeyPath = Path.ChangeExtension(filePath, null);
                if (File.Exists(privateKeyPath))
                {
                    processedPaths.Add(privateKeyPath);
                }
            }
            else
            {
                // If this is a PuTTY key or other format, mark corresponding public key as processed
                var publicKeyPath = filePath.Replace(".ppk", ".pub", StringComparison.OrdinalIgnoreCase);
                if (File.Exists(publicKeyPath))
                {
                    processedPaths.Add(publicKeyPath);
                }
            }
            
            yield return key;
        }

        // Then, look for private keys that don't have a corresponding .pub file
        // (these should be registered separately as they're standalone private keys)
        foreach (var filePath in Directory
                     .EnumerateFiles(baseSshPath, "*", SearchOption.TopDirectoryOnly)
                     .Where(e => !processedPaths.Contains(e) && 
                                 !e.EndsWith(".pub", StringComparison.OrdinalIgnoreCase) &&
                                 !e.EndsWith(".ppk", StringComparison.OrdinalIgnoreCase)))
        {
            // Skip common non-key files
            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            if (fileName == "config" || fileName == "known_hosts" || fileName == "authorized_keys")
            {
                continue;
            }

            // Check if it's actually a private key by trying to load it
            ISshKey? key = null;
            try
            {
                key = KeyFactory.FromPath(filePath);
            }
            catch (Exception)
            {
                // Not a valid SSH key file, skip it
                continue;
            }

            if (key != null && !(key is ISshPublicKey))
            {
                // This is a private key - check if it has a corresponding .pub file
                // If it does, it should have been processed already, so skip it
                var publicKeyPath = filePath + ".pub";
                if (!File.Exists(publicKeyPath))
                {
                    // This is a standalone private key without a .pub file
                    processedPaths.Add(filePath);
                    yield return key;
                }
            }
        }

        // Then, get keys from SSH config IdentityFile entries
        foreach (var identityFilePath in GetIdentityFilePathsFromConfig())
        {
            if (processedPaths.Contains(identityFilePath))
            {
                continue;
            }

            ISshKey? key = null;
            try
            {
                // Try to load the key from the IdentityFile path
                // IdentityFile typically points to a private key, but we prefer to show the public key if it exists
                var extension = Path.GetExtension(identityFilePath);
                
                // If the path has no extension, it's likely a private key - try to find the public key first
                if (string.IsNullOrEmpty(extension))
                {
                    var pubPath = identityFilePath + ".pub";
                    if (File.Exists(pubPath))
                    {
                        key = KeyFactory.FromPath(pubPath);
                        if (key != null)
                        {
                            processedPaths.Add(pubPath);
                            // Mark the corresponding private key as processed
                            processedPaths.Add(identityFilePath);
                        }
                    }
                }

                // If we don't have a key yet, try loading from the IdentityFile path directly
                if (key == null)
                {
                    key = KeyFactory.FromPath(identityFilePath);
                    if (key != null)
                    {
                        processedPaths.Add(identityFilePath);
                        // Mark the corresponding public key as processed if it exists
                        var pubPath = identityFilePath + ".pub";
                        if (File.Exists(pubPath))
                        {
                            processedPaths.Add(pubPath);
                        }
                    }
                }

                if (key is IPpkKey && convert) key = KeyFactory.ConvertToOppositeFormat(key, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while reading key from IdentityFile path {path}", identityFilePath);
            }

            if (key is null) continue;
            yield return key;
        }
    }

    /// <summary>
    ///     Retrieves all SSH keys from disk or cache asynchronously, using a yield return method to lazily load the keys.
    /// </summary>
    /// <param name="loadFromDisk">Optional. Indicates whether to load keys from disk. Default is false.</param>
    /// <param name="purgePasswords">Optional. Indicates whether to purge passwords from cache. Default is false.</param>
    /// <returns>An asynchronous enumerable collection of ISshKey representing the SSH keys.</returns>
    public static async IAsyncEnumerable<ISshKey> GetAllKeysYield(bool loadFromDisk = false,
        bool purgePasswords = false)
    {
        await using var dbContext = new OpenSshGuiDbContext();
        var cacheHasElements = await dbContext.KeyDtos.AnyAsync();
        var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // If loading from cache, return cached keys first
        if (!loadFromDisk && cacheHasElements)
        {
            foreach (var keyFromCache in dbContext.KeyDtos.Select(e => e.ToKey()))
            {
                if (keyFromCache != null)
                {
                    // Check if this key should be skipped
                    if (processedPaths.Contains(keyFromCache.AbsoluteFilePath))
                    {
                        continue;
                    }
                    
                    processedPaths.Add(keyFromCache.AbsoluteFilePath);
                    
                    // Mark the corresponding keypair file as processed to prevent duplicate registration
                    if (keyFromCache is ISshPublicKey)
                    {
                        // If this is a public key, mark the corresponding private key as processed
                        var privateKeyPath = Path.ChangeExtension(keyFromCache.AbsoluteFilePath, null);
                        if (File.Exists(privateKeyPath))
                        {
                            processedPaths.Add(privateKeyPath);
                        }
                        // Always yield public keys
                        yield return keyFromCache;
                    }
                    else if (keyFromCache.AbsoluteFilePath.EndsWith(".pub", StringComparison.OrdinalIgnoreCase))
                    {
                        // If file ends with .pub but wasn't recognized as ISshPublicKey, still mark private key
                        var privateKeyPath = Path.ChangeExtension(keyFromCache.AbsoluteFilePath, null);
                        if (File.Exists(privateKeyPath))
                        {
                            processedPaths.Add(privateKeyPath);
                        }
                        // Yield .pub files even if not recognized as ISshPublicKey
                        yield return keyFromCache;
                    }
                    else
                    {
                        // This is a private key - check if a corresponding public key exists
                        var publicKeyPath = keyFromCache.AbsoluteFilePath + ".pub";
                        if (File.Exists(publicKeyPath))
                        {
                            // Public key exists - skip this private key entry and let the public key entry handle it
                            // But first check if we already have the public key in our processed paths
                            if (!processedPaths.Contains(publicKeyPath))
                            {
                                // The public key hasn't been processed yet, so we need to load it instead
                                ISshKey? publicKey = null;
                                try
                                {
                                    publicKey = KeyFactory.FromPath(publicKeyPath, keyFromCache.Password);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error while loading public key from cache path {path}", publicKeyPath);
                                }
                                
                                if (publicKey != null)
                                {
                                    processedPaths.Add(publicKeyPath);
                                    yield return publicKey;
                                }
                                else
                                {
                                    // If we can't load the public key, fall back to yielding the private key
                                    yield return keyFromCache;
                                }
                            }
                            // If public key was already processed, we don't need to do anything
                        }
                        else
                        {
                            // No corresponding public key - this is a standalone private key
                            yield return keyFromCache;
                        }
                    }
                }
            }
        }

        // Always check SSH config file for IdentityFile entries, even when loading from cache
        // This ensures keys referenced in config are always discovered
        var convertPpk = (await dbContext.Settings.FirstAsync()).ConvertPpkAutomatically;
        foreach (var identityFilePath in GetIdentityFilePathsFromConfig())
        {
            if (processedPaths.Contains(identityFilePath))
            {
                continue;
            }

            ISshKey? key = null;
            try
            {
                // Try to load the key from the IdentityFile path
                var extension = Path.GetExtension(identityFilePath);
                
                // If the path has no extension, it's likely a private key - try to find the public key first
                if (string.IsNullOrEmpty(extension))
                {
                    var pubPath = identityFilePath + ".pub";
                    if (File.Exists(pubPath) && !processedPaths.Contains(pubPath))
                    {
                        key = KeyFactory.FromPath(pubPath);
                        if (key != null)
                        {
                            processedPaths.Add(pubPath);
                            // Mark the corresponding private key as processed
                            processedPaths.Add(identityFilePath);
                        }
                    }
                }

                // If we don't have a key yet, try loading from the IdentityFile path directly
                if (key == null)
                {
                    key = KeyFactory.FromPath(identityFilePath);
                    if (key != null)
                    {
                        processedPaths.Add(identityFilePath);
                        // Mark the corresponding public key as processed if it exists
                        var pubPath = identityFilePath + ".pub";
                        if (File.Exists(pubPath) && !processedPaths.Contains(pubPath))
                        {
                            processedPaths.Add(pubPath);
                        }
                    }
                }

                if (key is IPpkKey && convertPpk) key = KeyFactory.ConvertToOppositeFormat(key, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while reading key from IdentityFile path {path}", identityFilePath);
            }

            if (key == null) continue;

            // Check if key already exists in database
            var found = await dbContext.KeyDtos.FirstOrDefaultAsync(e =>
                e.AbsolutePath == key.AbsoluteFilePath);
            if (found is null)
            {
                await dbContext.KeyDtos.AddAsync(new SshKeyDto
                {
                    AbsolutePath = key.AbsoluteFilePath,
                    Password = key.Password,
                    Format = key.Format
                });
                await dbContext.SaveChangesAsync();
            }
            else
            {
                // Update key with cached password if available
                if (found.Password is not null) key.Password = found.Password;
                if (purgePasswords)
                {
                    key.Password = key.HasPassword ? "" : null;
                    found.Password = key.HasPassword ? "" : null;
                    await dbContext.SaveChangesAsync();
                }
            }

            yield return key;
        }

        // If loading from disk (not from cache), also load standard keys from ~/.ssh directory
        if (loadFromDisk || !cacheHasElements)
        {
            foreach (var key in GetFromDisk(convertPpk))
            {
                if (processedPaths.Contains(key.AbsoluteFilePath))
                {
                    continue;
                }

                var found = await dbContext.KeyDtos.FirstOrDefaultAsync(e =>
                    e.AbsolutePath == key.AbsoluteFilePath);
                if (found is null)
                {
                    await dbContext.KeyDtos.AddAsync(new SshKeyDto
                    {
                        AbsolutePath = key.AbsoluteFilePath,
                        Password = key.Password,
                        Format = key.Format
                    });
                }
                else
                {
                    found.AbsolutePath = key.AbsoluteFilePath;
                    found.Format = key.Format;
                    if (found.Password is not null) key.Password = found.Password;
                    if (purgePasswords)
                    {
                        key.Password = key.HasPassword ? "" : null;
                        found.Password = key.HasPassword ? "" : null;
                    }
                }

                await dbContext.SaveChangesAsync();
                yield return key;
            }
        }
    }

    /// <summary>
    ///     Retrieves all SSH keys from disk or cache.
    /// </summary>
    /// <param name="loadFromDisk">Optional. Indicates whether to load keys from disk. Default is false.</param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    public static IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        return GetAllKeysYield(loadFromDisk).ToBlockingEnumerable();
    }
}