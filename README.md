# Easy Bites

Easy Bites is an ASP.NET Core MVC web application targeting **.NET 9**. The project structure combines both backend and frontend assets in a single codebase following the default conventions of ASP.NET Core.

## Project Structure Overview

| Layer | Location | Description |
|-------|----------|-------------|
| **Backend** | `Program.cs`, `Controllers/`, `Models/` | C# source responsible for configuring the web host, handling HTTP requests, and business logic. |
| **Frontend (Server-Rendered)** | `Views/` | Razor (`*.cshtml`) views that are rendered on the server. |
| **Frontend (Static Assets)** | `wwwroot/` | Client-side assets served directly by Kestrel (CSS, JavaScript, images, fonts, etc.). |

```
EasyBites/
├── Program.cs           // Application entry-point & middleware pipeline
├── Controllers/         // MVC controllers (backend endpoints)
├── Models/              // View models and domain models
├── Views/               // Razor views (frontend templates)
└── wwwroot/             // Static files (frontend assets)
```

> **Tip:** Anything placed inside `wwwroot/` is publicly accessible. Everything else is compiled into the application and not directly exposed.

## Prerequisites

* [.NET 9 SDK](https://dotnet.microsoft.com/download) (preview as of writing)
* macOS, Windows, or Linux

## Running the Application Locally

```bash
# Restore NuGet packages
$ dotnet restore

# Build & run the development server
$ dotnet run
```

The server listens on `https://localhost:5001` (and `http://localhost:5000`). Open the URL in your browser to view the site.

## Publishing for Production

Publish a self-contained build for macOS (Apple Silicon):

```bash
$ dotnet publish -c Release -r osx-arm64 --self-contained true
```

The output will be in `bin/Release/net9.0/osx-arm64/publish/` and can be copied to any Mac without installing the .NET runtime.

---

Feel free to adapt this README to match your workflow or deployment environment. 