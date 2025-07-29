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
*   **Dynamic Serving Size Adjustment:** Adjust recipe serving sizes with automatic ingredient recalculation using AI-powered scaling.
*   **Recipe Variations:** Create, view, and manage recipe variations with different serving sizes and ingredient modifications.
*   **User Accounts:** Register, log in, and manage user profiles with cooking level, bio, favorite cuisine, and location.
*   **Recipe Submission:** Submit new recipes to the platform (subject to admin approval) with draft functionality.
*   **Saved Recipes:** Save favorite recipes for easy access and management.
*   **Recipe Progress Tracking:** Track progress while cooking a recipe (current step, checked-off ingredients).
*   **Recipe Ratings:** Rate recipes on a 1-5 scale.
*   **Feedback Submission:** Provide feedback on the platform with different types and ratings.
*   **Reporting:** Report inappropriate content or users with detailed descriptions.
*   **AI-Generated Recipe Images:** Generate and view AI-powered recipe images using Google Gemini.
*   **Temporary Image Generation:** Preview AI-generated images during recipe submission.

**For Administrators:**

*   **Comprehensive Dashboard:** View platform statistics (total users, total recipes, pending recipes, average ratings), recent activities, popular categories, and pending actions.
*   **Recipe Management:**
    *   View all recipes with advanced filtering options (status, search).
    *   Approve, reject, or edit submitted recipes.
    *   Delete recipes and associated data.
    *   Generate, regenerate, or update recipe images.
    *   Manage recipe status and publication.
*   **User Management:**
    *   View all users with search and filtering capabilities.
    *   Create new user accounts with admin privileges.
    *   Suspend, activate, or permanently ban user accounts.
    *   Retrieve user information and usernames.
*   **Feedback Management:** View, filter, and respond to user feedback with status updates.
*   **Report Management:** View, filter, manage, and respond to user-submitted reports with admin notes.
*   **Activity Logging:** Comprehensive tracking of all actions performed on the platform.
*   **Test Data Generation:** Create sample reports for testing purposes.

## Technical Stack

*   **Backend:** ASP.NET Core 9, C#
*   **Frontend:**
    *   Server-rendered views using Razor (`.cshtml`)
    *   Static HTML/CSS/JavaScript assets in `wwwroot/easybites-platform/` for client-side interactions
    *   Bootstrap 5 for responsive design
*   **Database & Backend Services:** Supabase (PostgreSQL, Storage)
*   **Authentication:** Custom cookie-based authentication system
*   **AI Services:** Google Gemini AI for image generation and recipe scaling
*   **Services:**
    *   `ActivityLogService` - Comprehensive activity tracking
    *   `GeminiService` - AI integration for images and content
    *   `RecipeImageService` - Recipe image management
    *   `SupabaseStorageService` - File storage management

## API Endpoints

The application provides comprehensive REST API endpoints:

*   **Authentication:** `/api/auth/*` - Registration, login, profile management
*   **Recipes:** `/api/recipes/*` - Recipe CRUD, search, filtering, variations, progress tracking
*   **Admin:** `/api/admin/*` - Administrative functions for users, recipes, feedback, reports
*   **Feedback:** `/api/feedback/*` - User feedback submission and management
*   **Reports:** `/api/reports/*` - Content and user reporting system

## Publishing for Production

Publish a self-contained build for macOS (Apple Silicon):

```bash
$ dotnet publish -c Release -r osx-arm64 --self-contained true
```

The output will be in `bin/Release/net9.0/osx-arm64/publish/` and can be copied to any Mac without installing the .NET runtime.

---

## Documentation

This README provides a general overview. For detailed documentation on specific functionalities, please refer to the comprehensive documentation in the `Docs/` directory:

*   **[Project Schema](Docs/Project%20schema.md)** - Complete database schema and table relationships
*   **[Authentication System](Docs/auth.md)** - User registration, login, profile management, and security
*   **[Recipe Management](Docs/recipes.md)** - Recipe CRUD operations, variations, progress tracking, and AI features
*   **[Admin Panel](Docs/admin.md)** - Administrative dashboard, user management, and content moderation
*   **[Feedback System](Docs/feedback.md)** - User feedback submission and administrative response management
*   **[Reports System](Docs/reports.md)** - Content and user reporting with administrative review workflow

Each documentation file provides detailed API endpoint specifications, data models, workflows, and implementation details for the respective system components.