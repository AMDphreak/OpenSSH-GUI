OpenSSH_GUI

A GUI for managing your SSH Keys - on Windows, Linux and macOS!

The primary reason for creating this project was to give "end-users"  
a modern looking GUI for managing their SSH Keys - and making it easier  
to deploy them to a server of their choice.

The program I found -> [PuSSHy](https://github.com/klimenta/pusshy) was, in my opinion  
not as user-friendly as it could be. I also wanted to use this program on my different  
machines, running on Linux and macOS. So I decided to create my own!

I hope you like it!

### Installing

No Installation needed! Just run the OpenSSHA_GUI.exe or .bin

## Usage

It is free to you, if you connect to a Server or not.  
This program can be used on PC's (Local Machines) and Servers!

If you choose to connect to a server - **_beware!_**  
This program - nor the author(s) take responsibility for saved messed up files!  
**_Make a backup if you already have files!_**

If you need help, open an [Issue]()

The program has a tooltip on every icon, describing what will happen  
if you click on it.

#### Main Window

![](images/MainWindow.png)

##### V2 UI

![](images/NewMainUI.png)  
You can now convert the Key to the opposite format.  
You can choose to delete or keep the key.  
If the key is kept, the program will move it into a newly created sub-folder of your  
.ssh directory.

##### Key without provided password

![](images/FoundPasswordProtectedKey.png)

##### Password options, when a password was provided

![](images/ShowForgetPws.png)

##### Provide password prompt

![](images/ProvidePasswordPrompt.png)

##### Application Settings

![](images/AppSettings.png)
App settings can be accessed through the settings context menu.
There is also an option, that the program converts all PPK keys in your .ssh directory  
to the OpenSSH format. The PPK Keys are not deleted, they will be put into a folder called PPK
![](images/SettingsContextMenu.png)

##### Sorting feature

You can sort the keys, if you want to. Just click on the top description category to sort by.
![](images/Sorted.png)

#### Add SSH Key

![](images/AddKeyWindow.png)

#### Connect to a Server

Right-Click on the Connection-status icon and click "Connect" on the showing menu.

![](images/ConnectToServerWindow.png)

- You can also auth with a public key from the recognized keys on your machine!  
  ![](images/ConnectToServerWindowWithKey.png)

- V2 Feature: Quick Connect
  ![](images/ConnectToServerQuickConnect.png)  
  If you submitted a valid connection earlier, the program will save the connection,  
  and suggest this connection here for quick access.

- You need to test the connection before you can submit it, if you do not use the new Quick-Connect feature.  
  If you get a connection error, an error window shows up.  
  ![](images/ConnectToServerWindowSuccess.png)

#### Edit Authorized Keys

Edit your local (or remote) authorized_keys!

![](images/EditAuthorizedKeysWindow.png)

In the remote Version you can even add a key from the recognized keys!
The key cannot be added, when it's already present on the remote!
![](images/EditAuthorizedKeysWindowRemote.png)

#### Edit Known Hosts Window

![](images/KnownHostsWindow.png)

Here you have a list of all "Known Hosts" from your "known_hosts" file.
If you want to remove one key from a Host, toggle the button of the specific Key.
If you want to remove the whole host, just toggle the button on the top label.

#### View Key Window

![](images/ExportKeyWindow.png)

The "View" buttons (formerly "Export") allow you to view and copy your public or private keys. You can also view key fingerprints by clicking the fingerprint button in the key list.

#### SSH Config Integration

**New Feature**: The application automatically discovers SSH keys referenced in your `~/.ssh/config` file!

- **Automatic Discovery**: On launch, the app parses your SSH config file and includes all keys specified in `IdentityFile` entries
- **Cross-Location Support**: Keys stored anywhere on disk (not just in `~/.ssh`) are automatically discovered and displayed
- **Path Resolution**: The app handles various path formats:
  - Absolute paths (`/path/to/key` or `C:\path\to\key`)
  - Relative paths (relative to `~/.ssh`)
  - Tilde expansion (`~/keys/mykey` or `~/.ssh/keys/mykey`)
  - Environment variables (`%USERPROFILE%\.ssh\key` or `$HOME/.ssh/key`)
  - Quoted paths (automatically handled)

All discovered keys appear in the main interface alongside keys from the standard `~/.ssh` directory.

#### Enhanced Key List Features

- **Path Column**:
  - Shows the full file path to each key
  - Text is selectable and horizontally scrollable within the cell
  - Columns are properly sized by importance, not content width
- **Fingerprint Access**:
  - Click the fingerprint button to view the full key fingerprint in a separate window
  - Fingerprints are now accessible on-demand rather than cluttering the main view
- **Improved Layout**:
  - Fixed-width columns for Key Type, Fingerprint button, and Export/Delete actions
  - Variable-width Path column with minimum width constraint
  - Responsive design that maintains proper alignment when resizing
  - All controls have appropriate minimum sizes to prevent UI distortion

#### Tooltips

**_Tooltip when not connected to a server_**  
![](images/tooltip.png)

**_Tooltip from Key_**  
![](images/tooltipKey.png)

**_Tooltip from connection_**  
![](images/tooltipServer.png)

## Further Information

- The program will create these at startup without prompting if they don't exist:  
  .ssh/(**authorized_keys**, **known_hosts**)  
   (.config/OpenSSH_GUI/ | AppData\Roaming\OpenSSH_GUI\) **OpenSSH_GUI** and a "logs" directory

- **Key Discovery**: The application discovers SSH keys from:
  - Standard `~/.ssh` directory (files with `.pub` or `.ppk` extensions)
  - All `IdentityFile` entries in your `~/.ssh/config` file (regardless of location)
  - Keys are automatically loaded on launch and displayed in the interface

### Attention: This program will save your Passwords!

You can not disable this feature. The Passwords are stored when:

- you enter a server connection with a password
- provide a password for a keyfile

Your passwords are stored on your local machine inside the SQLite Database, protected with AES-Encryption.  
Only the program itself can read any kind of string value inside the database.

## Recent Improvements

- [x] **SSH Config Integration**: Automatically discovers keys from `~/.ssh/config` IdentityFile entries
- [x] **Enhanced Path Column**: Selectable and horizontally scrollable text with proper column sizing
- [x] **Fingerprint View**: Moved to on-demand window access for cleaner interface
- [x] **Improved Layout**: Fixed-width columns, proper constraints, and responsive design
- [x] **Better UI Stability**: Minimum sizes prevent UI distortion during window resizing

## Plans for the future

- [x] ~~Add functionality for putting a key onto a Server~~
- [x] ~~Beautify UI~~ (Improved layout and constraints)
- [x] ~~Add functionality for editing authorized_keys~~
- [ ] Add functionality for editing local and remote SSH (user/root) Settings
- [x] ~~Add functionality for editing application settings~~
- [x] ~~Servers should be saved and quickly accessed in the connect window.~~
- [x] ~~Discover keys from SSH config file~~
- many more not yet known!

## Authors

- **Oliver Schantz** - _Idea and primary development_ -
  [GitHub](https://github.com/frequency403)

See also the list of
[contributors](https://github.com/frequency403/OpenSSH-GUI/contributors)
who participated in this project.

## Used Libraries / Technologies

- [Avalonia UI](https://avaloniaui.net/) - Reactive UI

- [ReactiveUI.Validation](https://github.com/reactiveui/ReactiveUI.Validation/)

- [MessageBox.Avalonia](https://github.com/AvaloniaCommunity/MessageBox.Avalonia)

- [Material.Icons](https://github.com/SKProCH/Material.Icons)

- [SSH.NET](https://github.com/sshnet/SSH.NET)

- [Serilog](https://serilog.net/)

- [SshNet.Keygen](https://github.com/darinkes/SshNet.Keygen/)

- [SshNet.PuttyKeyFile](https://github.com/darinkes/SshNet.PuttyKeyFile)

- [EntityFrameworkCore](https://github.com/dotnet/EntityFramework.Docs)

- [EntityFrameworkCore.DataEncryption](https://github.com/Eastrall/EntityFrameworkCore.DataEncryption)

- [SQLite](https://sqlite.org/)

## License

This project is licensed under the [MIT License](LICENSE)

- see the [LICENSE](LICENSE) file for
  details
