#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Static;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Represents an SSH public key.
/// </summary>
public class SshPublicKey(string absoluteFilePath, string? password = null)
    : SshKey(absoluteFilePath, password), ISshPublicKey
{
    /// <summary>
    ///     Represents the private key associated with this public key.
    ///     May be null if the private key file doesn't exist or cannot be loaded.
    /// </summary>
    public ISshKey? PrivateKey { get; } = LoadPrivateKey(absoluteFilePath, password);

    /// <summary>
    ///     Exports the private key in OpenSSH format by delegating to the PrivateKey property.
    /// </summary>
    /// <returns>The private key in OpenSSH format as a string, or null if the private key doesn't exist.</returns>
    public new string? ExportOpenSshPrivateKey()
    {
        return PrivateKey?.ExportOpenSshPrivateKey();
    }

    /// <summary>
    ///     Attempts to load the corresponding private key for this public key.
    /// </summary>
    /// <param name="publicKeyPath">The path to the public key file.</param>
    /// <param name="password">The password for the private key, if any.</param>
    /// <returns>The private key if it exists and can be loaded; otherwise, null.</returns>
    private static ISshKey? LoadPrivateKey(string publicKeyPath, string? password)
    {
        try
        {
            var privateKeyPath = Path.ChangeExtension(publicKeyPath, null);
            if (!File.Exists(privateKeyPath))
            {
                // Private key file doesn't exist, return null - the public key can still be used
                return null;
            }
            return KeyFactory.FromPath(privateKeyPath, password);
        }
        catch
        {
            // If we can't load the private key, return null but don't crash
            return null;
        }
    }
}