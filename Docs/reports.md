# Reports System (`ReportsController.cs` and `AdminController.cs`)

The reports system allows users to report inappropriate content or users, and provides administrators with tools to manage and respond to these reports.

## User-Facing Functionalities (`ReportsController.cs`):

*   **Submit Report (`POST /api/reports/submit`):**
    *   Allows authenticated users to submit reports about inappropriate content or users.
    *   Requires `report_type` and `description`.
    *   Optionally accepts `reported_user_id` or `reported_recipe_id` depending on what is being reported.
    *   Associates the report with the authenticated user as the reporter.
    *   Saves the report to the `reports` table with a default `status` of "pending".
    *   Logs the report submission activity.
    *   Returns a success message and the ID of the submitted report.

*   **Get My Reports (`GET /api/reports/my-reports`):**
    *   Retrieves all reports submitted by the currently authenticated user.
    *   Returns a list of the user's reports with their current status and details.
    *   Allows users to track the status of their submitted reports.

## Admin-Facing Functionalities (`AdminController.cs`):

*   **Get All Reports (`GET /api/admin/reports`):**
    *   Retrieves all reports for administrators to review.
    *   Supports filtering by `status` (e.g., "pending", "reviewed", "resolved") and `type`.
    *   Supports pagination (`page`, `limit`).
    *   Returns detailed report data including reporter information, reported content/user details, and timestamps.

*   **Get Report Details (`GET /api/admin/reports/{id}`):**
    *   Retrieves detailed information for a specific report.
    *   Includes all report fields, related user information, and any admin notes or responses.
    *   Used by administrators to review reports before taking action.

*   **Update Report (`PUT /api/admin/reports/{id}`):**
    *   Allows administrators to update a report's status and add administrative notes.
    *   Typically used to change the `status` (e.g., to "reviewed", "resolved", "dismissed").
    *   Allows adding `admin_notes` to document the administrative decision or action taken.
    *   Sets `reviewed_at` timestamp and `reviewed_by_admin_id`.
    *   Logs this administrative activity.

*   **Create Report (`POST /api/admin/reports`):**
    *   Allows administrators to create reports on behalf of users or the system.
    *   Useful for administrative reporting or system-generated reports.
    *   Logs the report creation activity.

*   **Create Test Reports (`POST /api/admin/reports/test-data`):**
    *   A utility endpoint for administrators to generate sample report data for testing purposes.
    *   Helps populate the system with test data during development or demonstration.

## Supporting Model:

*   `Report` (Model): Defines the structure for report entries stored in the `reports` table. Key fields include:
    *   `id` (Primary Key)
    *   `reporter_user_id` (FK to users table - who submitted the report)
    *   `reported_user_id` (FK to users table - user being reported, optional)
    *   `reported_recipe_id` (FK to recipes table - recipe being reported, optional)
    *   `report_type` (e.g., "inappropriate_content", "spam", "harassment")
    *   `description` (detailed description of the issue)
    *   `status` (e.g., "pending", "reviewed", "resolved", "dismissed")
    *   `created_at`
    *   `reviewed_at`, `reviewed_by_admin_id`, `admin_notes` (Admin-specific fields)

## Report Types:

The system supports various types of reports:
*   **Content Reports:** Reports about inappropriate recipes, descriptions, or images
*   **User Reports:** Reports about user behavior, harassment, or policy violations
*   **Spam Reports:** Reports about spam content or fake accounts
*   **Other:** Custom report types as needed

## Workflow:

1.  A user encounters inappropriate content or behavior and submits a report via `POST /api/reports/submit`.
2.  The report is stored in the `reports` table with `status: "pending"`.
3.  The user can track their reports via `GET /api/reports/my-reports`.
4.  Administrators can view all pending reports via `GET /api/admin/reports`.
5.  An administrator reviews a report using `GET /api/admin/reports/{id}` for detailed information.
6.  The administrator takes appropriate action and updates the report status using `PUT /api/admin/reports/{id}`.
7.  The `ActivityLogService` records all report-related activities for audit purposes.

## Security & Privacy:

*   Only authenticated users can submit reports
*   Users can only view their own submitted reports
*   Administrative functions require admin authentication
*   All report activities are logged for audit trails
*   Reporter identity is protected in the administrative interface