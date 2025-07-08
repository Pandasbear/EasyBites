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

## Application Capabilities

Easy Bites is a feature-rich recipe platform with the following key capabilities:

**For Users:**

*   **Recipe Discovery:** Browse, search, and filter recipes based on various criteria (keywords, category, difficulty, cooking time).
*   **Recipe Viewing:** View detailed recipe information, including ingredients, instructions, preparation time, cooking time, servings, tips, and nutritional information.
*   **User Accounts:** Register, log in, and manage user profiles.
*   **Recipe Submission:** Submit new recipes to the platform (subject to admin approval).
*   **Saved Recipes:** Save favorite recipes for easy access.
*   **Recipe Progress Tracking:** Track progress while cooking a recipe (e.g., current step, checked-off ingredients).
*   **Feedback Submission:** Provide feedback on the platform or specific recipes.
*   **Reporting:** Report inappropriate content or users.
*   **AI-Generated Recipe Images:** View AI-generated images for recipes.

**For Administrators:**

*   **Dashboard:** View platform statistics (total users, total recipes, pending recipes, average ratings) and recent activities.
*   **Recipe Management:**
    *   View all recipes with filtering options.
    *   Approve, reject, or edit submitted recipes.
    *   Delete recipes.
    *   Generate or regenerate AI recipe images.
*   **User Management:**
    *   View all users with search and filtering options.
    *   Create new users.
    *   Suspend, activate, or ban user accounts.
*   **Feedback Management:** View, filter, and respond to user feedback.
*   **Report Management:** View, filter, and manage user-submitted reports.
*   **Activity Logging:** Track various actions performed on the platform.

## Technical Stack

*   **Backend:** ASP.NET Core 9, C#
*   **Frontend:**
    *   Server-rendered views using Razor (`.cshtml`)
    *   Static assets (HTML, CSS, JavaScript) in `wwwroot/` for client-side interactions.
*   **Database & Backend Services:** Supabase (PostgreSQL, Storage, Auth - though custom auth is implemented)
*   **AI Image Generation:** Google Gemini AI

## Publishing for Production

Publish a self-contained build for macOS (Apple Silicon):

```bash
$ dotnet publish -c Release -r osx-arm64 --self-contained true
```

The output will be in `bin/Release/net9.0/osx-arm64/publish/` and can be copied to any Mac without installing the .NET runtime.

---

This README provides a general overview. For more detailed documentation on specific functionalities, please refer to the files in the `Docs/` directory.