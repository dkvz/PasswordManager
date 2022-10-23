# Password Manager Tools

Command line app. that was mainly created to generate the initial encrypted password database file.

Linked to my Password Manager web app project: https://github.com/dkvz/PasswordManagerApp

Careful that this isn't super secure as the memory page will still dangle somewhere once the program is closed. It's better to run the script on the safest computer you can get and then modify it using the actual app, as the app never stores the password in clear text (although it briefly stores it as a byte array).

## Argument parsing
I'm interested in the Prompt tools also provided by [the CommandLineUtils package](https://github.com/natemcmaster/CommandLineUtils) so I'm using that. It pretty much defines the whole flow of the app.

Installing the package:
```
dotnet add package McMaster.Extensions.CommandLineUtils
```

Add the using statement:
```cs
using McMaster.Extensions.CommandLineUtils;
```
There are two ways to use the package, I'm doing the "Attribute API" way which has less arrow functions and more descriptive options.

The auto-usage message actually works. To see it with `dotnet run` you need to add a "--" argument before because ``dotnet run` has its own "-h" argument which has priority.

So this will show the help message for the console app:
```
dotnet run -- -h
```

## Building the executable
Using `dotnet build` creates a dll that you can run with `dotnet run` but that's not helping.

It looks like you need to specify a target to build an executable.

Let's test these:
```
dotnet publish -c Release -r win10-x64
dotnet publish -c Release -r ubuntu.16.10-x64
```

## JSON stuff
Even Microsoft recommends using a NuGet package from here: https://www.newtonsoft.com/json

Installing:
```
dotnet add package Newtonsoft.Json
```

# AES stuff
There are AES directives in the base library although it's a little hairy to implement, as I expected it to be.

The [documentation here](https://webman.developpez.com/articles/dotnet/aes-rijndael/) seems helpful enough. At the end they mention how to generate both the IV and the key using the same... Key (passphrase, password, whatever).

And then I also found a ["managed" example](https://www.c-sharpcorner.com/article/aes-encryption-in-c-sharp/) although I'm not sure what it means. I also think we should encode in base64 at the end.

## Simple.AES
I'm going to try using the following Nuget package: https://www.nuget.org/packages/Simple.AES/ ; We should make sure we use a specific version:
```
dotnet add package Simple.AES --version 2.0.2
```

The package normally uses a config file with the encryption parameters and key but I think we can provide that ourselves as an object.

I eventually gave up on Simple.AES because it's not been built for .Net Core.

## Alternatives
* https://gitlab.com/czubehead/csharp-aes
* https://github.com/jonjomckay/dotnet-simpleaes

First one is by far the simplest but the second one looks more secure in how it handles the strings and stuff but it gives the "built with not-donet-core" warning. So I'll use the second one.

I don't think it's not the most secure thing ever even though it does salt the key. The passwords file should be first added to a password protected 7z file before moving around.

With specific version:
```
dotnet add package SimpleAES --version 1.1.1
```

At some point I just copy pasted the class into my own namespace and added methods that take a byte array as the key.

We should also add the possibility of choosing the hash algorithm at some point for the key derivation thingy. It's using SHA1 by default.

# Windows + VScode setup
OmniSharp won't work unless you install the "Visual Studio Build Tools" which should be somewhere on the [Visual Studio download page](https://visualstudio.microsoft.com/downloads/).

You then have to select the .NET Core stuff which was on the bottom of the installer Window for me (VS 2019) - It's 2.9 GB by the way.

# TODO
- [x] Can I actually load the compiled assembly (a .dll I think) in my other project? -> You can but I'm referencing it as an entire project instead (gets compiled alongside the other project).
- [x] There is a more secure Random, although it might only generate bytes. I should think of using it -> For what we do with Random it's fine.
- [x] Add a warning when using the reading mode (which is the default) that all passwords will be displayed in the console.
- [ ] When doing Delete or Modify : Allow providing the entry name in argument of the CLI.
- [x] Copy the SimpleAES library, adapt it, then remove the now useless dependency - Credit original author
- [ ] Write tests
- [ ] The code would be much clearer if we only allowed using byte array passwords instead of both that plus strings.
- [ ] Think of some sort of way to change the master password for an already existing file.
