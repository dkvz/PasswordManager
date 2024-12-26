# DkVZ Password Manager
Several projects parts of my own personal password manager, which is a web app using both a .NET backend and the JavaScript security API.

This used to be a .NET project with a dependency as a submodule and no .sln files, because I created it on Linux and the CLI tools will basically allow you to use any project layout.

The migration explains why there are so few commits in the project as parent projects have been made private.

There's been another migration after that to upgrade from .NET 2.2 to 6.0. Which was an adventure alright.

## Dependencies / Requirements
- NodeJS 12+
- .NET SDK 6.0

I use Parcel.JS to bundle static assets. Since Parcel weighs more than 90MB I started installing it globally, so it's not in package.json and is called through npx.

Make sure parcel is installed globally:
```
npm install -g parcel-bundler
```

Since I've had breaking changes issues with Parcel I should just use it as a normal dependency at some point.

## Running the web app & build assets
First install the client dependencies:
```
npm install
```

Then use the following to watch the client files and run dotnet in watch mode for the Web App:
```
npm run dev
```

Running on Arch requires exporting some weird variable to avoid getting an error about ICU not existing:
```
export CLR_ICU_VERSION_OVERRIDE=71.1
```
I guess that should be fixed someday.

It's not, and now my version is:
```
export CLR_ICU_VERSION_OVERRIDE=75.1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
```

OK at this point I just reinstalled .NET 6 and it worked.

## Building for production
It's discussed in a later section, I haven't tried building to production with the new project layout yet.

## Project migration
Just documenting it in case I need to do the same thing later.

To create a solution, inside the new solution directory:
```
dotnet new sln
```

I then cloned the various projects, removed their .git directory, moved around the current README, .gitignore, removed the old git submodule structure. The Tests project was in that submodule directory for some reason.

Each project has to be added to the solution:
```
dotnet sln add PasswordManagerTools
dotnet sln add Tests
dotnet sln add PasswordManagerApp
```

I thought now wasn't the time to try a .NET version update so I had to manually install the 2.2 SDK (it's no longer supported and not in Manjaro's official repos).

Try running the tests:
```
dotnet test Tests
```
It will give you a whole bunch of warnings, that's normal. The tests should all pass though.

-> Project files have to be edited to change the paths for their other project dependencies.

-> My npm scripts in PasswordManagerApp have to change.

I'm going to try the "multiple workspaces" npm thingy to hopefuly be able to install and run npm scripts from the project root.

I think it works, all of the modules appear to be at the root.

# Web App
I had to reinitialize the project because I initially started working on the older Razor template which has JQuery and a Carousel on the home page. Yeah I'm not kidding.

They ditched the carousel but Bootstrap and JQuery are still there.

Created the project using the "razor" template as in:
```
dotnet new razor
```
And this is where I should probably have used PascalCase for the root directory. I also found out I probably should have created a sln and multiple projects inside that sln. Oh well.

I added the CLI project I started that has some of the model classes using a git submodule (I'm so sorry) into the submodules folder.

**Make sure to manually checkout the submodule directory or nothing will work**.

Then added the project to the main .csproj using:
```
dotnet add reference submodules/PasswordManagerTools/PasswordManagerTools.csproj
```

We then need to tell the compiler to stop trying to watch all the files inside submodules, which is achieved by adding this to our main csproj:
```xml
<PropertyGroup>
  <DefaultItemExcludes>$(DefaultItemExcludes);submodules\**</DefaultItemExcludes>
</PropertyGroup>
```

## Configuration
TODO

## Building for production

### In short
My target hosting environment is Linux, behind a reverse proxy.

Without specifying a build target you can create a .dll assembly that will run on any platform on which there is a .NET Core runtime installed.

You could also create a self-contained deployment that ships with the .NET Core runtime and so does not have any requirement.

I think I'm going to go for that route. I thought you needed to add the `--self-contained true` switch but from what I've seen in Github issues, self-contained is on by default.

Also I get a weird bug if I force it telling me that I need to provide a runtime identifier, when I have provided one.
The "bug" might be happening because I'm referencing the PasswordManagerTools project in the main project.

To create the production build:
```
npm run build-linux
```

- You have to copy the "publish" directory that should be somewhere along the lines of `bin\Release\netcoreapp2.2\debian-x64` for the current build target I'm using - You should probably zip the thing because it's made of a LOT of different small files.
- In that directory there is an executable called "PasswordManagerApp", make it executable.
- Create the production config file by copying `appsettings.Development.json` into `appsettings.Production.json`.
- Give that file to the user that will run the app (www-data in my case) and chmod it to 700.
- Adapt the production config, you should remove all the "logging" stuff as the default has the verbosity we want, also set the right secret sequence you want to use and make sure the SMTP / notifications settings are correct.
- Create var/data and put at least a password file in there (you can generate one with [the CLI project](https://github.com/dkvz/PasswordManagerTools)). Make sure the directory is writable by the user that will run the service (I use www-data).
- Reference the password file in appsettings.Production.json.
- You can also chmod the password file 700.

#### Systemd service
You can now create a systemd service for the app.

You can copy and adapt the script below (check destination directory) into `/etc/systemd/system/password-manager.service`.
```ini
[Unit]
Description=Password Manager .NET Core App

[Service]
WorkingDirectory=/srv/vhosts/password_manager/current
ExecStart=/srv/vhosts/password_manager/current/PasswordManagerApp
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=PasswordManager
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```
I copied the script from [there](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-2.2#monitor-the-app).

If the service can start correctly you will want to register it to start with the server:
```
systemctl enable password-manager
```

By default it should log to syslog, and so the app output will end up in /var/log/syslog.

You can use rsyslog to split the file to its own separate log (app will still log to /var/log/syslog as well):
- Create a file called "password-manager.conf" in /etc/rsyslog.d/
- Add the following content:
```
:programname, isequal, "PasswordManager" /var/log/password-manager.log
```
- Reload the rsyslog service

The log file might not get rotated, you can configure that with logrotate.

#### Changes for modern distributions
I compile using .NET6 and use the "publish" directory as mentioned above. However, it won't start for several reasons.

- The env variable `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` is necessary to be able to run and should be added to the systemd script.

- We need a version of `libssl` - On Debian the following works: `apt intall libssl-dev`.

- Don't forget to have the relevant `appsettings.json` file ready with the required configuration keys for notifications, secret sequence, etc.

#### Reverse proxy configuration
I'm going to use the simplest configuration I can.

Just keep in mind that if your dotnet app is listening to port 5001 you can use that one in HTTPS and make it harder to try and compromise passwords by capturing packets on the loopback interface.

Otherwise, the HTTP port is 5000.

### Using Docker
I added a Dockerfile which requires a few steps first:

- Run `npm run build-linux` from the project root.
- Prepare a working `application.Production.json` file.
- Prepare the .pwd files you want to use.

The app runs on port 5000 internally.

Once a container is started I manually copy the required files to it (a volume or bind mount could be used for the data directory) using `docker copy`.

I pushed a public image at `dkvz/password-manager:latest` on the Docker Hub.

### References
These two pages have a lot of useful information:
* https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-cli
* https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-2.2

### Early build options
**NB:** Build options are actually quite complex, a lot is discussed below.

Use the following to create a Windows exe:
```
dotnet publish --configuration Release -r win-x64
```
The build is in the `bin/Release/netcoreapp2.2/<TARGET>/publish directory`. There are a lot of files in there, one of which is executable.

The command initially gave me an error in that it was supposed to use donet Core 2.2.0 but would be using 2.2.2 instead.

There are two possible fixes in the .csproj. The first one is to specify both the target runtime version, and the runtime identifies you want to use. They provided the following example (supposed to go in the first PropertyGroup):

```xml
<RuntimeFrameworkVersion>2.1.1</RuntimeFrameworkVersion>
<PlatformTarget>AnyCPU</PlatformTarget>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

What I'm doing it just adding the following line:
```xml
<TargetLatestRuntimePatch>true</TargetLatestRuntimePatch>
```

Actually it took me forever to notice the issue came from the submodule .csproj, the PasswordManagerTools project.

I just had to also add the TargetLatestRuntimePatch to the .csproj over there and publish worked.

Except my app still redirects to HTTPS, so I commented the line that does that in Startup.cs.
More importantly, the static assets are not there at all.

For what I want to do I'm pretty sure I need to use OutOfProcess hosting and not InProcess (which means we're supposed to put it inside IIS or something). OutOfProcess is supposed to be the default and uh... Yeah I'm not sure if it's even using that option but let's overwrite it anyway in the csproj:
```xml
<AspNetCoreHostingModel>OutOfProcess</AspNetCoreHostingModel>
```

The static content is still absent from the exe. I found [a page](https://www.learnrazorpages.com/publishing/publish-to-iis#including-static-content-in-the-root-folder) that seems to explain my issue.

The "good" news is that this confirms the app is not bundling/minifying anything in that state. It's possible to add a NuGet package to do it, or we do it ourselves. It could even be done in a separate project.

I'm going to need npm for client encryption packages so I might as well setup a bundler like Parcel. More on that later.

### Runtime identifiers
There is a list of runtime identifiers here: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog

You can give a runtime identifier to the "-r" option of ``dotnet publish`, that's what we did for the useless "build-win" script that was in package.json (and might still be in there).

For my Linux build I'm just going to use "linux-x64".

### Reverse proxy consideration
To make sure we get the right client IP we have to make it so that the .NET app is aware of X-Forwarded-* headers.

In Startup.cs, in the Configure method, we need to add a new middleware:
```cs
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
  ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

Which requires the following using statement:
```cs
using Microsoft.AspNetCore.HttpOverrides;
```

The app will only trust proxies calling from localhost. See [here](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-2.2#configure-a-reverse-proxy-server) for information about trusting other proxies.


## Layout modifications
I had to heavily modify the existing pages since I don't want Bootstrap, JQuery etc.

Just to remember these tags exist, they were using environment tags to do different things according to the build stage:
```html
<environment include="Development">
  <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.css" />
</environment>
<environment exclude="Development">
  <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css"
        asp-fallback-href="~/lib/bootstrap/dist/css/bootstrap.min.css"
        asp-fallback-test-class="sr-only" asp-fallback-test-property="position" asp-fallback-test-value="absolute"
        crossorigin="anonymous"
        integrity="sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T"/>
</environment>
```

Then there's the cookie warning partial (lel):
```html
<partial name="_CookieConsentPartial" />
```

I also disabled the cookie consent thing in Startup.cs. It appears in ConfigureServices and is then added as a middleware.

## Doing REST with Razor pages
It's possible. Although you still need a "Controller". I think it's probably best to just use one these Controller things.

Otherwise, check this out: https://www.learnrazorpages.com/web-api

We can easily scaffold a controller using a tool that we need to install first:
```
dotnet tool install --global dotnet-aspnet-codegenerator
```

Then that generator should work out for us:
```
dotnet-aspnet-codegenerator -p "C:\MyProject\MyProject.csproj" controller -name MyDemoModelController -api -m My.Namespace.Models.MyDemoModel -dc MyDemoDbContext -outDir Controllers -namespace My.Namespace.Controller
```

But I think I'm going to skip to GraphQL immediately, see following section.

## GraphQL
**NB**: At some point I just decided not to use GraphQL. I have very few endpoints and the convoluted security stuff I'm stringing in would make everything a little too crazy.

We're going to need the NuGET package from here: https://graphql-dotnet.github.io/

We can add the package to the csproj file using the following command:
```
dotnet add package GraphQL
```

Then [this article](https://medium.com/@mczachurski/graphql-in-net-core-project-fb333241ff0a) explains how to create a controller to provide the graphql endpoint.

There is an example GraphQL API from the author [here](https://github.com/BibliothecaTeam/Bibliotheca.Server.Gateway/tree/master/src/Bibliotheca.Server.Gateway.Api).

Since that guy uses some sort of dependency injection thingy I turned to [another article](https://fullstackmark.com/post/17/building-a-graphql-api-with-aspnet-core-2-and-entity-framework-core).
That article also explains how to install GraphiQL. But I won't be usin git.

### The schema
I got a list of names and associated passwords.

We should create the types associated with that schema, which is pretty simple.

We're going to create a basic class called `Models/PasswordEntry` and its related GraphQL type: `Models/PasswordEntryType`.

We should then create the Query and Mutation objects resolvers thingies accessors of whatnot.

* Models/PasswordManagerQuery
* Models/PasswordManagerMutation

And finally: Models/PasswordManagerSchema.

## The API
I should probably document it at some point. I might not.

All the endpoints are in `ApiController.cs`. Client-side code is in `api.js`.

They all use the POST method.

## Assets bundling
I think the easiest plan is to npm init the project directory and reference bundled files in `_Layout.cshtml` in the link and script tags (output to wwwroot and the right subdirectories).

We should then be able to rely on the `asp-append-version="true"` to help us with cache busting as it adds a ?v= query to the URL. We'll have to check if that actually works with the release package.

I think it's possible to create tasks to run before `dotnet build` takes place but we might as well do everything from npm.

I need to include wwwroot to the final build. There is a Folder entry to add to the csproj as shown in [this example](https://github.com/NetCoreTemplates/parcel/blob/master/MyApp/MyApp.csproj).

So I added this section to my csproj:
```xml
<ItemGroup>
  <Folder Include="wwwroot\" />
</ItemGroup>
```

For future reference, if we want to use the bundler to add a version string and include that script regardless of the version or hash it's possible too, using a tag such as:
```html
<script asp-src-include="~/js/app_*.js"></script>
```

### Parcel
I thought of using Rollup this time but I just reverted to Parcel.

What we want to do is create some sort of source directory that outputs to wwwroot. I chose to use "src" although it's a tiny bit confusing.

We can import the CSS in the JS entry point and Parcel will output a CSS file as well.

The "src" directory should be ignored by the dotnet compiler, which we can ensure it is by modifying the DefaultItemExclude (that we should already have since we also exclude "submodules"):

```xml
<PropertyGroup>
  <DefaultItemExcludes>$(DefaultItemExcludes);submodules\**;src\**</DefaultItemExcludes>
</PropertyGroup>
```

## Client-side encryption
I'm going to have to be able to encrypt and decrypt similarily between my two different AES libraries.

In JS I think I'm going to use the [aes-js](https://www.npmjs.com/package/aes-js) package.

I carried out experiments on [another repository](https://github.com/dkvz/node-encryption-experiment) although this is specific to Node and will have to be adapted to the browser. We'll also need to check for the presence of the window.crypto API.

-> I put the unsupported browser check in Index.cshtml.

We need a few libraries for which we add a specific version:
```
npm install -D --save-exact aes-js@3.1.2
npm install -D --save-exact pbkdf2@3.0.17
```

There is a problem with pbkdf2 which is using Buffer, which does not exist in browsers. It's apparently "polyfilled" by some Browserify addon.

If I really want to use Buffer, [there is a Buffer](https://www.npmjs.com/package/buffer) npm package that looks promising enough.

Or, I might just use something that is native to the crypto API with examples I found here: https://github.com/diafygi/webcrypto-examples#pbkdf2

You need to importKey first, the deriveKey.

I made a horrible pen that seems to work:
```js
const key = 'test';

// Need to convert the string to byte array.

// When you're a normal person you use TextEncoder.

const enc = new TextEncoder();

window.crypto.subtle.importKey(
    "raw", //only "raw" is allowed
    enc.encode(key), //your password
    {
        name: "PBKDF2",
    },
    false, //whether the key is extractable (i.e. can be used in exportKey)
    ["deriveKey", "deriveBits"] //can be any combination of "deriveKey" and "deriveBits"
)
.then(function(key){
    //returns a key object
    console.log('Imported key: ' + key);
    window.crypto.subtle.deriveBits(
    {
        "name": "PBKDF2",
        salt: window.crypto.getRandomValues(new Uint8Array(16)),
        iterations: 10000,
        hash: {name: "SHA-1"}, //can be "SHA-1", "SHA-256", "SHA-384", or "SHA-512"
    },
    key, //your key from generateKey or importKey
    256 //the number of bits you want to derive
    )
    .then(function(bits){
        //returns the derived bits as an ArrayBuffer
        console.log(new Uint8Array(bits));
    })
    .catch(function(err){
        console.error(err);
    });
})
.catch(function(err){
    console.error(err);
});
```

## Sessions cleanup
I'm implementing my own session mechanic using a singleton that stays in memory for the app lifetime.

For the automatic cleaning up I should look up background tasks: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.2

## App. configuration file
I'm probably going to need some way to configure the password database paths (app will allow declaring multiple ones).

[This Stackoverflow answer](https://stackoverflow.com/questions/31453495/how-to-read-appsettings-values-from-json-file-in-asp-net-core) offers a lot of details including how to to dependency injection of the config object.

And the official documentation: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2

It's possible to create one config file per environment (dev and prod that is). I'm probably going to do that and let people create the prod config file.

Btw the sequence is supposed to be a secret. Not as secret as the master password, but still secret. Hide the one you use in prod and change it sometimes.

The dataPath key identifies where the data files are supposed to be stored.

```json
{
  "sequence":"2,1;2,2;2,3",
  "dataPath": "var/data/",
  "dataFiles": [
    "main.pwd"
  ]
}
```

Since we use `CreateDefaultBuilder` in Startup.cs, it's actually supposed to automatically pick up all the appsettings.json files (base one which would have priority when they get merged (I think they get merged?), and the two with environment names).

For some reason the app created the json files when I ran it.

It's possible to manually bind a class to the configuration data (or part of it) but I'm going more generic using a more general way of getting the values as shown in the following example (which should also inject the configuration):
```cs
public class IndexModel : PageModel
{
  public IndexModel(IConfiguration config)
  {
    _config = config;
  }

  public int NumberConfig { get; private set; }

  public void OnGet()
  {
    NumberConfig = _config.GetValue<int>("NumberKey", 99);
  }
}
```

## Testing
It looks like you're normaly supposed to have a separate "test project". I could put it in submodules/ since I'm already using a separate project in there, that should NOT be in there.

I should've created a "sln" at the start and adding projects to it. Instead I created a project using the "razor" template, and added another in submodules while making sure that directory gets ignored by the compiler for the current project.

It's kind of a mess.

It might be possible to include the testing to the same project automatically, I still have to try it.

```
cd submodules
mkdir Tests
cd Tests
dotnet new nunit
```

Now we have to reference the test project into the main project (not necessary but will make `dotnet test` work in the root directory) and also reference the razor project from the test project.

I tried adding the reference to the Tests project inside the main csproj but it doesn't make the `dotnet test` command work from the root, so I deleted it.

In Tests.csproj:
```xml
<ItemGroup>
  <ProjectReference Include="../../PasswordManagerApp.csproj" />
</ItemGroup>
```

To run the tests I need to do:
```
dotnet test submodules/Tests
```

To make it easier I added that as an npm script and thus can use:
```
npm test
```

# Security considerations
It would actually be better to have the app being called through HTTPS but for some reason the self contained published app doesn't bind the HTTPS port on Linux (it seems to do it on Windows).

By intercepting packets on the loopback address on the server itself and with access to the secret sequence, it's possible to extract the master password from a full session communication.

This is of course also possible through extensive memory dumping but that is **much** harder to pull off.

I'm going to add a todo post to try and get the HTTPS Kestrel support.

# TODO
- [ ] Put parcel back as a dependency, I've had breaking changes making me work overtime to fix it.
- [ ] Add some sort of check that shows a warning if the connection is not in HTTPS.
- [ ] Double check if the ClientIp we save in SecureSession objects works with X-Forwarded-For when deployed in production, because there's some chance it doesn't.
- [ ] SessionManager is not thread safe. But I think that would be one of the worst cost/benefit change I could make.
- [ ] I got rid of the Consent cookie but now there's another one called "anti forgery" or something. What is that about?
- [ ] I do not know what happens if some of the source strings provided are empty - I should check for empty data in the API endpoints.
- [ ] Lock an IP address that does too many failed login attempts.
- [ ] We could hook into some possible "tab got focus" event that would check if we should automatically disconnect or not.
- [ ] In the JS code there are byte arrays I could clean up from memory at some point but I usually don't bother.
- [ ] Add a button to clear the clipboard.
- [ ] Rather than using Console.Error.WriteLine et al. in many places I should inject the ILogger and use that.
- [ ] Try to get kestrel to serve HTTPS with the Linux production build as well. For some reason it's not binding https://localhost:5001 with the self-contained Linux build. I have to test by running the dll with dotnet and not using the self-contained approach to see if that fixes it.
- [ ] I should have some sort of error callback for the email notifications and/or add an API endpoint only available on localhost that sends a test email.
- [x] Remove the old project from Github -> Made it private.
- [x] Use CSS variables while I'm at it.
- [x] There doesn't seem to be a handler for error 404s -> Quick fixed this by adding `app.UseStatusCodePages();``in Startup.cs. Not awesome but it works.
- [x] Email notifications for failed login attempts, log all the successful logins somewhere.
- [x] Log all of the attempts somwhere.
- [x] The `asp-append-version="true"` thing doesn't work at all with the production release, the version ID's are gone. -> It does work, the correct exe is in PasswordManagerApp\bin\Release\netcoreapp2.2\win-x64\publish or equivalent.
- [x] I have a 404 on the source maps - They don't seem to be available through Kestrel, probably because they're referenced as being at the root in the files (as in /sites.css.map instead of /assets/sites.css/map) (we juste need to add --public-url to parcel).
- [x] Add Babel just for the fun of it and also because my cheap browser check in Index.cshtml encompasses browsers that have no ES6 support -> Parcel just auto babelifies ES6.
- [x] Uses or Random in PasswordManagerTools should be replaced with the secured version - It's a TODO item in that project as well -> For what it's used over there it does'nt matter.
- [x] A cookie called .AspNet.Consent is sent with requests. We might want to get rid of it.
- [x] TestRequest.cs should be removed.
- [x] Re-test the whole session clean up thing.
- [x] The file selected at login has to be sanitized before it's used on the backend. We should probably just send the position in the list.
- [x] Change the title when the view changes.
- [x] Add some sort of spinner when doing the API requests.
- [x] There should also be some sort of spinner while we're initializing the index page and processing the JS in site.js.
- [x] I don't think the calls to System.GC.Collect() do anything super helpful in the PasswordManagerTools project. I feel like they're slowing everything down by a lot. I should remove them.
- [x] The password list (HTML select element) looks terrible. Can we do something with the CSS? -> Not really if using the select element as is.
- [x] Add a margin left to the close icon for the toaster message.
- [x] App. directory structure is messed up. I should have a sln project referencing all the others (including the Test project) and have better naming for the directories with C# or JS code.
- [x] Show an unsaved changes warning message (using the warning colours) in the notification section when saves need to happen, also show the save button (hide otherwise).
- [x] To open a new session I created something in SessionManager that returns an enum member. To save the session I did it almost entirely in ApiController. I should be consistent here and pick one or the other. Some methods in ISessionManager won't be needed anymore after the refactoring.
- [x] I'm not super sure what happens if a password is longer than 16 characters. It should pad to always be a multiple of 16 bytes but I should test it. In the same vein I also need to test a password that is exactly 16 characters to see if my JS de-padding works in that case too.
- [x] The password field on the second slide show the number of characters in the password; I should probably use placeholder text or find an option to hide the number of characters.
- [x] The copy to clipboard thingy should first look if hiddenPasswordInput.value is empty and copy the visible field instead in that case.
