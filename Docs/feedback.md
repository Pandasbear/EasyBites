# Feedback System (`FeedbackController.cs` and `AdminController.cs`)

The feedback system allows users to submit various types of feedback and administrators to manage and respond to it.

## User-Facing Functionalities (`FeedbackController.cs`):

*   **Submit Feedback (`POST /api/feedback/submit`):**
    *   Allows any user (authenticated or anonymous) to submit feedback.
    *   Requires feedback `type`, `subject`, and `message`.
    *   Optionally accepts `name`, `email`, and a `rating` (integer).
    *   Saves the feedback to the `feedback` table with a default `status` of "new".
    *   Returns a success message and the ID of the submitted feedback.

*   **Get All Feedback (`GET /api/feedback` - Admin use, but in `FeedbackController`):**
    *   Retrieves a list of all feedback entries.
    *   Supports filtering by feedback `type` (case-insensitive).
    *   This endpoint seems intended for admin use based on its functionality, though it's located in the general `FeedbackController`.

## Admin-Facing Functionalities (`AdminController.cs`):

*   **Get All Feedback (`GET /api/admin/feedback`):**
    *   Retrieves feedback entries for administrators.
    *   Supports filtering by `status` (e.g., "new", "reviewed"), `type`, and `rating`.
    *   Supports pagination (`page`, `limit`).
    *   Returns detailed feedback data.

*   **Update Feedback (`PUT /api/admin/feedback/{id}`):**
    *   Allows administrators to update a feedback entry.
    *   Typically used to change the `status` (e.g., to "reviewed", "resolved"), add an `admin_response`.
    *   Sets `reviewed_at` timestamp and `reviewed_by_admin_id`.
    *   Logs this activity.

## Supporting Model:

*   `Feedback` (Model): Defines the structure for feedback entries stored in the `feedback` table. Key fields include:
    *   `id` (Primary Key)
    *   `name`, `email` (Optional submitter info for anonymous feedback)
    *   `type` (e.g., "bug", "suggestion", "compliment", "complaint")
    *   `subject`, `message` (Required feedback content)
    *   `rating` (Optional integer rating, typically 1-5 scale)
    *   `status` (e.g., "new", "reviewed", "resolved", "archived")
    *   `submitted_at` (Timestamp of submission)
    *   Note: Some admin-specific fields (`reviewed_at`, `reviewed_by_admin_id`, `admin_response`) are currently commented out in the model due to schema cache issues but are supported in the database schema.

## Workflow:

1.  A user submits feedback via `POST /api/feedback/submit`.
2.  The feedback is stored in the `feedback` table with `status: "new"`.
3.  Administrators can view new and existing feedback via `GET /api/admin/feedback`.
4.  An administrator reviews a feedback item and updates its status and adds a response using `PUT /api/admin/feedback/{id}`.
5.  The `ActivityLogService` records when feedback is reviewed.
