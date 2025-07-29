# Admin Panel (`AdminController.cs`)

The Admin Panel provides administrators with tools to manage the Easy Bites platform, including users, recipes, feedback, and reports. Access to these functionalities is restricted to authenticated admin users.

## Key Admin Functionalities:

### Dashboard:

*   **Get Dashboard Stats (`GET /api/admin/dashboard/stats`):**
    *   Provides comprehensive aggregate statistics including:
        *   Total users, total recipes, pending recipes
        *   Average recipe rating across the platform
        *   User activity metrics and growth trends
*   **Get Recent Activities (`GET /api/admin/dashboard/activities`):**
    *   Retrieves a chronological list of recent platform activities.
    *   Logged by the `ActivityLogService` with detailed action tracking.
    *   Includes user actions, admin actions, and system events.
*   **Get Popular Categories (`GET /api/admin/dashboard/popular-categories`):**
    *   Shows the top 5 most popular recipe categories.
    *   Based on the count of approved recipes in each category.
    *   Helps administrators understand content trends.
*   **Get Pending Actions (`GET /api/admin/dashboard/pending-actions`):**
    *   Comprehensive summary of items requiring admin attention:
        *   Pending recipe approvals
        *   Flagged or inactive user accounts
        *   Unread feedback submissions
        *   Pending reports requiring review

### Recipe Management:

*   **Get All Recipes (`GET /api/admin/recipes`):**
    *   Lists all recipes, with optional filtering by `status` (e.g., "pending", "approved", "rejected").
    *   Supports pagination.
*   **Get Recipe Details (`GET /api/admin/recipes/{id}`):**
    *   Retrieves full details for a specific recipe.
*   **Update Recipe (`PUT /api/admin/recipes/{id}`):**
    *   Allows admins to modify any aspect of a recipe, including its content and `status`.
    *   Logs the update activity.
*   **Generate Recipe Image (`POST /api/admin/recipes/{id}/generate-image`):**
    *   Triggers AI image generation for a recipe via `RecipeImageService`.
    *   Updates the recipe's `image_url`.
    *   Logs the activity.
*   **Update Recipe Image URL (`PUT /api/admin/recipes/{id}/image-url`):**
    *   Allows admins to manually set or change a recipe's image URL.
    *   Logs the activity.
*   **Update Recipe Status (`PUT /api/admin/recipes/{id}/status`):**
    *   Specifically for changing a recipe's `status` (e.g., "approved", "rejected").
    *   Logs the activity.
*   **Delete Recipe (`DELETE /api/admin/recipes/{id}`):**
    *   Permanently deletes a recipe and its associated data (progress, saved references).
    *   Logs the activity.

### User Management:

*   **Create User (`POST /api/admin/users`):**
    *   Allows admins to create new user accounts, specifying details like name, email, username, password, and admin status.
    *   Logs the activity.
*   **Get All Users (`GET /api/admin/users`):**
    *   Lists all users, with optional filtering by `status` (active/suspended) and `search` term.
    *   Supports pagination.
*   **Get Username by ID (`GET /api/admin/users/{id}/username`):**
    *   Retrieves the username for a given user ID.
*   **Update User Status (`PUT /api/admin/users/{id}/status`):**
    *   Allows admins to `suspend`, `activate`, or `ban` a user.
    *   "Suspend" and "Activate" toggle the `active` flag in `users` and `user_profiles`.
    *   "Ban" permanently deletes the user from `users` and `user_profiles`.
    *   Logs the activity.

### Feedback Management:

*   **Get All Feedback (`GET /api/admin/feedback`):**
    *   Lists all feedback entries, with optional filtering by `status`, `type`, and `rating`.
    *   Supports pagination.
*   **Update Feedback (`PUT /api/admin/feedback/{id}`):**
    *   Allows admins to update feedback `status`, add an `admin_response`, and mark it as reviewed.
    *   Logs the activity.

### Reports Management:

*   **Get All Reports (`GET /api/admin/reports`):**
    *   Lists all user-submitted reports, with optional filtering by `status` and `type`.
    *   Supports pagination.
*   **Get Report Details (`GET /api/admin/reports/{id}`):**
    *   Retrieves details for a specific report.
*   **Update Report (`PUT /api/admin/reports/{id}`):**
    *   Allows admins to update report `status`, add `admin_notes`, and mark it as reviewed.
    *   Logs the activity.
*   **Create Report (`POST /api/admin/reports`):**
    *   Though primarily a user action, this endpoint also allows admins (or the system) to create reports.
    *   Logs the activity.
*   **Create Test Reports (`POST /api/admin/reports/test-data`):**
    *   A utility endpoint for admins to generate sample report data for testing.
    *   Useful for development, demonstration, and system testing purposes.
    *   Creates realistic test data that follows the same structure as real reports.

## Authentication & Authorization:

*   Admin access is controlled by the `GetCurrentAdminUser()` helper method, which verifies:
    1.  A valid `session_id` cookie.
    2.  The session exists in the `user_sessions` table and is marked as an admin session (`IsAdmin = true`).
    3.  The session has not expired.
    4.  The associated user in the `users` table also has `IsAdmin = true`.
*   Most admin endpoints will return `Unauthorized` if these conditions are not met.

## Supporting Services:

*   `ActivityLogService`: Used extensively to log admin actions.
*   `RecipeImageService`: Used for managing recipe images.
*   Direct interaction with Supabase client for data manipulation across various tables (`users`, `recipes`, `feedback`, `reports`, `user_sessions`).

## Key Models Involved:

*   `User` (from `AuthController`)
*   `Recipe`
*   `Feedback`
*   `Report`
*   `ActivityLog`
*   `UserSession` (from `AuthController`)
*   Various DTOs for request and response data specific to admin operations.
