# Authentication (`AuthController.cs`)

The authentication system manages user registration, login, sessions, and profile management.

## Key Functionalities:

*   **User Registration (`POST /api/auth/register`):**
    *   Allows new users to create an account by providing first name, last name, email, username, password, and optionally cooking level, bio, favorite cuisine, and location.
    *   Validates input (e.g., password match, password length, unique email/username).
    *   Hashes passwords using SHA256.
    *   Creates a new user record in the `users` table and a corresponding `user_profiles` record.
    *   Logs the registration event.

*   **User Login (`POST /api/auth/login`):**
    *   Allows registered users to log in using their email/username and password.
    *   Verifies credentials against the `users` table.
    *   Checks if the user account is active.
    *   Creates a session using ASP.NET Core cookie authentication.
    *   Updates the `last_login` timestamp in the `user_profiles` table.
    *   Logs the login event.

*   **Admin Login (`POST /api/auth/admin-login`):**
    *   Allows admin users to log in using username, password, and a special admin security code.
    *   Verifies credentials and admin status in the `users` table.
    *   Creates a session using ASP.NET Core cookie authentication and also stores a record in the `user_sessions` table with an `IsAdmin` flag.
    *   Logs the admin login event.

*   **Get Current User (`GET /api/auth/me`):**
    *   Retrieves details of the currently authenticated user based on the session cookie.
    *   Returns user information including profile details and whether an admin session is active.

*   **Logout (`POST /api/auth/logout`):**
    *   Logs out the currently authenticated user by clearing the session cookie.

*   **Update Profile (`PUT /api/auth/profile`):**
    *   Allows authenticated users to update their profile information including:
        *   Personal details: first name, last name, username
        *   Cooking preferences: cooking level, favorite cuisine
        *   Additional info: bio, location
    *   Updates records in both `users` and `user_profiles` tables.
    *   Validates username uniqueness and other constraints.

*   **Change Password (`PUT /api/auth/password`):**
    *   Allows authenticated users to change their password after verifying their current password.
    *   Uses SHA256 hashing for password security.
    *   Updates the `password_hash` in the `users` table.
    *   Requires current password verification for security.

*   **Delete Account (`POST /api/auth/delete-account`):**
    *   Allows authenticated users to permanently delete their account after password verification.
    *   Comprehensive data cleanup including:
        *   User records from `users` and `user_profiles` tables
        *   Associated recipes, saved recipes, and progress tracking
        *   User sessions and activity logs
    *   Automatically logs the user out after successful deletion.
    *   Irreversible operation with security confirmation required.

## Supporting Models:

*   `User`: Main user entity stored in the `users` table.
*   `UserProfile`: Additional user profile details stored in the `user_profiles` table.
*   `UserSession`: Stores admin session information in the `user_sessions` table.
*   Various DTOs for request and response data.

## Security:

*   Passwords are hashed using SHA256.
*   Session management is handled by ASP.NET Core's cookie authentication.
*   Admin login requires an additional security code.
*   Endpoint authorization is applied to restrict access where necessary.
