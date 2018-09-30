# Windows 10 IoT Core App Updates Service
We needed a way to remotely update .NET Core 2.x apps (not UWP apps) installed on devices running Windows 10 IoT Core.  We created a standard Windows service that periodically reads an app update manifest from a secure remote URI and updates locally installed apps on the device as needed.

### Current Features
1. The app update manifest and associated app update packages can be hosted on the local file system for testing, a website, Azure storage, Dropbox, One Drive, Google Drive, etc.
2. Configurable daily check time (defaults to 3am). 
3. The service can update multiple apps.
4. Package integrity is validated using a MD5 hash.
5. The update manager will automatically "kill" processes being updated and restart them after they've been updated.
6. The server side administrative website functionality can be integrated into any existing ASP.NET Core 2.x project by simply adding the Hyprsoft.IoT.AppUpdates.Web NuGet package and making the startup class configuration changes noted below. 

### Process to Update an App
1. Add a new package definition to the manifest with an incremented file version, new release date, new source URI, and new checksum.
2. Upload the app update package (i.e. zip file) containing the updated application files (EXEs, DLLs, etc.) to your desired "host" (i.e  file sharing service).
3. The next time the service checks for updates the new package definition is detected and the package is automatically downloaded to the device and installed.

### Sample Service Configuration
```json
{
  "CheckTime": "03:00:00",
  "NextCheckDate": "2018-09-18T03:00:00",
  "ManifestUri": "http://www.hyprsoft.com/app-update-manifest.json",
  "InstalledApps": [
    {
      "ApplicationId": "04fc007e-db18-430f-b4fa-f5b54de1e142",
      "InstallUri": "c:\\testapp"
    }
  ]
}
```
### Sample Manifest
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
        "SourceUri": "http://www.hyprsoft.com/packages/hyprsoft.my.awesome.app-1000.zip",
        "Checksum": "bddf0cd85b9b4985fb10a1555e10ab6d"
      },
      {
        "Id": "02902554-4d3d-4159-b106-41e0ac158733",
        "IsAvailable": true,
        "Version": "1.0.1.0",
        "ReleaseDateUtc": "2018-09-16T00:00:00",
        "SourceUri": "http://www.hyprsoft.com/packages/hyprsoft.my.awesome.app-1010.zip",
        "Checksum": "e6276631b2c67664810132f1ee565d59"
      }
    ]
  }
]
```
### Sample Startup.cs Configuration (optional)
The administrative website can be accessed using http[s]://[www.your-domain.com]/appupdates.
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
### Security Concerns
By default the app update service runs on the device under the 'NT AUTHORITY\SYSTEM' user context and has full rights/access to the operating and file system.  This means that the processes the service invokes after an update also run under the same unrestricted user context. **This can be a security risk!**

### Admin Website Screen Shots
#### Login
![Login](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/login-thumb.png)

#### Apps - List
![Apps - List](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/apps-list-thumb.png)

#### Apps - Add
![Apps - Add](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/apps-add-thumb.png)

#### Apps - Edit
![Apps - Edit](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/apps-edit-thumb.png)

#### Packages - Add
![Packages - Add](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/packages-add-thumb.png)

#### Packages - Edit
![Packages - Edit](https://github.com/hyprsoftcorp/IoTCoreAppUpdates/blob/master/Media/packages-edit-thumb.png)
