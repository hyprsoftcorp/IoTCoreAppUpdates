# Windows 10 IoT Core App Updates Service
We needed a way to remotely update .NET Core 2.1 apps (not UWP apps) installed on IoT devices running Windows 10 IoT Core.  We created a standard Windows service that periodically reads an app update manifest from a remote host and updates locally installed apps on the device as needed.  Please note that this service was designed for and has only been tested with .NET Core 2.1 binaries.

### Current Features
1. The app update manifest and associated app update packages can be hosted on any website, Azure storage, Dropbox, One Drive, Google Drive, etc.
2. Configurable daily check time (defaults to 3am). 
3. The service can update multiple apps.
4. Package integrity is validated using a MD5 hash.
5. The update manager will automatically "kill" processes being updated and restart them after they've been updated.
6. The server side administrative website functionality can be integrated into any existing ASP.NET Core 2.1 project by simply adding the Hyprsoft.IoT.AppUpdates.Web NuGet package and making the startup class configuration changes noted below.
7. If the Hyprsoft.IoT.AppUpdates.Web NuGet package is utilized, update package endpoints are automatically protected using simple bearer token authentication.  This prevents unauthorized downloads of your app packages.
8. Credentials for accessing the administrative website can be supplied using Azure App Service settings or Azure Key Vault.

### Process to Update an App
#### Automatically
1. Integrate the administrative website NuGet package into your existing ASP.NET Core 2.1 website and manage updates and packages via your browser.  See the sample startup.cs code and administrative website screenshots below.

#### Manually
1. Add a new package definition to the app update manifest with an incremented file version, new release date, new source URI, and new checksum.
2. Upload the app update package (i.e. zip file) containing the updated application files (EXEs, DLLs, etc.) to your desired "host" (i.e  website or file sharing service).

### Sample Service Configuration
The service configuration Json file resides on each device.
```json
{
  "IsAuthenticationRequired": true,
  "CheckTime": "03:00:00",
  "NextCheckDate": "2018-09-18T03:00:00",
  "ManifestUri": "http://www.yourdomain.com/app-update-manifest.json",
  "InstalledApps": [
    {
      "ApplicationId": "04fc007e-db18-430f-b4fa-f5b54de1e142",
      "InstallUri": "c:\\data\\testapp"
    }
  ]
}
```
### Sample App Update Manifest
The app update manifest Json file and app packages can reside on a website or any file sharing service.
```json
[
  {
    "Id": "04fc007e-db18-430f-b4fa-f5b54de1e142",
    "Name": "My Awesome App",
    "Description": "My Awesome App Description",
    "ExeFilename": "hyprsoft.my.awesome.app.exe",
    "VersionFilename": "hyprsoft.my.awesome.app.dll",
    "CommandLine": "param1",
    "Packages": [
      {
        "Id": "02902554-4d3d-4159-b106-41e0ac158733",
        "IsAvailable": true,
        "Version": "1.0.0.0",
        "ReleaseDateUtc": "2018-09-01T00:00:00",
        "SourceUri": "http://www.yourdomain.com/appupdates/apps/04fc007e-db18-430f-b4fa-f5b54de1e142/packages/download/hyprsoft.my.awesome.app-1000.zip",
        "Checksum": "bddf0cd85b9b4985fb10a1555e10ab6d"
      },
      {
        "Id": "02902554-4d3d-4159-b106-41e0ac158733",
        "IsAvailable": true,
        "Version": "1.0.1.0",
        "ReleaseDateUtc": "2018-09-16T00:00:00",
        "SourceUri": "http://www.yourdomain.com/appupdates/apps/04fc007e-db18-430f-b4fa-f5b54de1e142/packages/download/hyprsoft.my.awesome.app-1010.zip",
        "Checksum": "e6276631b2c67664810132f1ee565d59"
      }
    ]
  }
]
```
### Sample Program.cs
If you would like to incorporate the app updates administrative website functionality into your own website, you need to install the Hyprsoft.Iot.AppUpdates.Web NuGet package and configure your program.cs like below.  Once integrated and deployed, the administrative website can be accessed using http[s]://[www.your-domain.com]/appupdates.
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseAppUpdates();
}
```
### Sample Startup.cs
If you have incorporated the app updates administrative website functionality into your own website, you need to make the following code changes.
```csharp
public class Startup
{
    public Startup(IConfiguration configuration, IHostingEnvironment env)
    {
        Configuration = configuration;
        HostingEnvironment = env;
    }

    public IConfiguration Configuration { get; }

    public IHostingEnvironment HostingEnvironment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAppUpdates(options => options.ManifestUri = new Uri(Path.Combine(HostingEnvironment.WebRootPath, UpdateManager.DefaultAppUpdateManifestFilename)));
        services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
    }

    public void Configure(IApplicationBuilder app)
    {
        if (HostingEnvironment.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseExceptionHandler("/Home/Error");

        app.UseAppUpdates();
        app.UseMvc(routes => routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}"));
    }
}
```
### Sample Web.config for Hosting in IIS/Azure App Service
If you have incorporated the app updates administrative website functionality into your own website, you need to make the following code changes.
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.web>
    <!-- in kilobytes-->
    <httpRuntime maxRequestLength="51200" />
  </system.web>
  <system.webServer>
    <security>
      <requestFiltering>
        <!--in bytes-->
        <requestLimits maxAllowedContentLength="52428800" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
```
### General Usage Notes
1. When dealing with versions of EXE and DLL files it is important to note that the "File Version" is used as opposed to the "Product Version". 

![File Properties](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/file-properties.jpg)

2. When the update manager searches for the latest package to install, the package's ReleasedDateUtc is used instead of the assembly file version.  So the package with the most recent ReleasedDateUtc will be chosen regardless of the assembly file version.
3. File checksums can be manually calculated using the [Microsoft File Checksum Integrity Verifier](https://www.microsoft.com/en-us/download/details.aspx?id=11533) utility.  Ex: fciv.exe -MD5 hyprsoft.my.awesome.app-1010.zip

### Security Concerns
By default the app update service runs on the IoT device under the 'NT AUTHORITY\SYSTEM' user context and has full rights/access to the operating and file systems.  This means that the processes the service invokes after an update also run under the same unrestricted user context. **This can be a security risk!  Use at your own risk!**

### Administrative Website Credentials
Credentials can be provided in two ways.  Both require adding Azure App Service settings.
#### Azure App Service
Setting Name | Example Value
--- | ---
AppUpdatesUsername | myusername
AppUpdatesPassword | myp@ssw0rd

#### Azure Key Vault
Setting up a key vault is tedious and outside the scope of this document but here is a summary of the process:
1. Setup an App Registration in AAD and note the Application ID.  This is the AppUpdatesKeyVaultClientId.
2. Add a password key to the app registration and note it's value (displayed only on creation).  This is the AppUpdatesKeyVaultClientSecret.
3. Add a secret to the key vault for the username and note the secret identifier.  This is the AppUpdatesKeyVaultUsernameSecret.
4. Add a secret to the key vault for the password and note the secret identifier.  This is the AppUpdatesKeyVaultPasswordSecret.

Setting Name | Example Value
--- | --- 
AppUpdatesKeyVaultClientId | bce7fe0f-09a4-4ded-be30-372c0e90d9e5
AppUpdatesKeyVaultClientSecret | aS/zHfJ118SOKwvEQ1sWzMc2fkreFG+s+qtz5oQUbks=
AppUpdatesKeyVaultUsernameSecret | https://mykeyvault.vault.azure.net/secrets/AppUpdatesUsername/4d10318a536c13e4ae37c1571fc88d1c
AppUpdatesKeyVaultPasswordSecret | https://mykeyvault.vault.azure.net/secrets/AppUpdatesPassword/d00a91afcc4d45b196e2c1de94b21a4b

### Administrative Website Screenshots
#### Login
![Login](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/login-thumb.png)

#### List of defined Apps
![List of defined Apps](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/apps-list-thumb.png)

#### Add a new App
![Add a new App](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/apps-add-thumb.png)

#### Edit an existing App
![Edit an existing App](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/apps-edit-thumb.png)

#### Add an app update package
![Add an app update package](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/packages-add-thumb.png)

#### Edit an app update package
![Edit an app update package](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/packages-edit-thumb.png)
