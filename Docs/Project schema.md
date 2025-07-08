-- WARNING: This schema is for context only and is not meant to be run.
-- WARNING: This schema is for context only and is not meant to be run directly.
-- It's inferred from the application's models and may not reflect all database constraints or exact Supabase setup.
-- Table order and constraints may not be valid for direct execution.

-- Stores user account information.
CREATE TABLE public.users (
  id uuid NOT NULL DEFAULT gen_random_uuid(), -- Primary key, unique identifier for the user.
  first_name text,                            -- User's first name.
  last_name text,                             -- User's last name.
  email text NOT NULL UNIQUE,                 -- User's email address, must be unique. Used for login.
  username text NOT NULL UNIQUE,              -- User's username, must be unique. Used for login and display.
  password_hash text NOT NULL,                -- Hashed password for security.
  cooking_level text,                         -- User's self-assessed cooking skill level (e.g., "Beginner", "Intermediate").
  is_admin boolean DEFAULT false,             -- Flag indicating if the user has administrative privileges.
  bio text,                                   -- Short biography or description about the user.
  favorite_cuisine text,                      -- User's favorite type of cuisine.
  location text,                              -- User's location.
  profile_image_url text,                     -- URL to the user's profile picture.
  created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP, -- Timestamp of when the user account was created.
  admin_security_code text,                   -- Special code for admin login, if applicable.
  active boolean NOT NULL DEFAULT true,       -- Flag indicating if the user account is active or suspended.
  CONSTRAINT users_pkey PRIMARY KEY (id)
);

-- Stores detailed information about recipes.
CREATE TABLE public.recipes (
  id uuid NOT NULL DEFAULT gen_random_uuid(), -- Primary key, unique identifier for the recipe.
  name text NOT NULL,                         -- Name of the recipe.
  description text,                           -- Detailed description of the recipe.
  category text,                              -- Category of the recipe (e.g., "Dinner", "Dessert").
  difficulty text,                            -- Difficulty level (e.g., "Easy", "Medium", "Hard").
  prep_time integer,                          -- Preparation time in minutes.
  cook_time integer,                          -- Cooking time in minutes.
  servings integer,                           -- Number of servings the recipe makes.
  ingredients TEXT[],                         -- List of ingredients (stored as an array of text).
  instructions TEXT[],                        -- List of cooking instructions (stored as an array of text).
  tips text,                                  -- Optional tips or variations for the recipe.
  nutrition_info text,                        -- Nutritional information (e.g., calories, macros).
  dietary_options TEXT[],                     -- Dietary tags (e.g., "Vegan", "Gluten-Free").
  author text,                                -- Name of the recipe author (can be user's username or manually entered).
  submitted_at timestamp with time zone DEFAULT now(), -- Timestamp of when the recipe was submitted.
  status text DEFAULT 'pending'::text,        -- Current status (e.g., "pending", "approved", "rejected", "draft").
  total_time integer GENERATED ALWAYS AS (prep_time + cook_time) STORED, -- Calculated total time (prep + cook). Supabase might handle this differently, this is a standard SQL way.
  user_id uuid,                               -- Foreign key referencing the user who submitted the recipe.
  image_url text,                             -- URL to the recipe's image.
  is_draft boolean NOT NULL DEFAULT false,    -- Flag indicating if the recipe is a draft.
  CONSTRAINT recipes_pkey PRIMARY KEY (id),
  CONSTRAINT recipes_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL -- If user is deleted, recipes remain but user_id is nulled, or handle differently (e.g. ON DELETE CASCADE).
);

-- Stores ratings given by users to recipes.
CREATE TABLE public.ratings (
  id uuid NOT NULL DEFAULT gen_random_uuid(), -- Primary key for the rating.
  recipe_id uuid NOT NULL,                    -- Foreign key referencing the recipe being rated.
  user_id uuid NOT NULL,                      -- Foreign key referencing the user who gave the rating.
  score integer NOT NULL CHECK (score >= 1 AND score <= 5), -- Rating score from 1 to 5.
  created_at timestamp with time zone DEFAULT now(), -- Timestamp of when the rating was given.
  CONSTRAINT ratings_pkey PRIMARY KEY (id),
  CONSTRAINT ratings_recipe_id_fkey FOREIGN KEY (recipe_id) REFERENCES public.recipes(id) ON DELETE CASCADE, -- If recipe is deleted, delete ratings.
  CONSTRAINT ratings_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE,    -- If user is deleted, delete their ratings.
  CONSTRAINT ratings_user_recipe_unique UNIQUE (user_id, recipe_id) -- Ensures a user can rate a recipe only once.
);

-- Stores feedback submitted by users.
CREATE TABLE public.feedback (
  id uuid NOT NULL DEFAULT gen_random_uuid(), -- Primary key for the feedback.
  name text,                                  -- Name of the person submitting feedback (optional).
  email text,                                 -- Email of the submitter (optional).
  type text NOT NULL,                         -- Type of feedback (e.g., "bug", "suggestion").
  subject text NOT NULL,                      -- Subject of the feedback.
  message text NOT NULL,                      -- Content of the feedback message.
  rating integer,                             -- Optional rating associated with the feedback.
  status text DEFAULT 'new'::text,            -- Status of the feedback (e.g., "new", "reviewed", "resolved").
  submitted_at timestamp with time zone DEFAULT now(), -- Timestamp of submission.
  reviewed_at timestamp with time zone,       -- Timestamp of when an admin reviewed the feedback.
  reviewed_by_admin_id uuid,                  -- Foreign key referencing the admin who reviewed it.
  admin_response text,                        -- Response from the admin.
  CONSTRAINT feedback_pkey PRIMARY KEY (id),
  CONSTRAINT feedback_reviewed_by_admin_id_fkey FOREIGN KEY (reviewed_by_admin_id) REFERENCES public.users(id) ON DELETE SET NULL -- If admin user is deleted, keep feedback but nullify reviewer.
);

-- Stores reports made by users about recipes or other users.
CREATE TABLE public.reports (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL, -- Primary key for the report.
  reporter_user_id uuid,                      -- Foreign key referencing the user who made the report.
  reported_user_id uuid,                      -- Foreign key referencing the user being reported (if applicable).
  reported_recipe_id uuid,                    -- Foreign key referencing the recipe being reported (if applicable).
  report_type text NOT NULL,                  -- Type of report (e.g., "spam", "inappropriate_content").
  description text NOT NULL,                  -- Detailed description of the report.
  status text DEFAULT 'pending'::text,        -- Status of the report (e.g., "pending", "reviewed", "resolved").
  created_at timestamp with time zone DEFAULT now(), -- Timestamp of when the report was created.
  reviewed_at timestamp with time zone,       -- Timestamp of when an admin reviewed the report.
  reviewed_by_admin_id uuid,                  -- Foreign key referencing the admin who reviewed it.
  admin_notes text,                           -- Notes made by the admin during review.
  CONSTRAINT reports_pkey PRIMARY KEY (id),
  CONSTRAINT reports_reporter_user_id_fkey FOREIGN KEY (reporter_user_id) REFERENCES public.users(id) ON DELETE SET NULL, -- If reporting user deleted, keep report.
  CONSTRAINT reports_reported_user_id_fkey FOREIGN KEY (reported_user_id) REFERENCES public.users(id) ON DELETE SET NULL, -- If reported user deleted, keep report.
  CONSTRAINT reports_reported_recipe_id_fkey FOREIGN KEY (reported_recipe_id) REFERENCES public.recipes(id) ON DELETE SET NULL, -- If reported recipe deleted, keep report.
  CONSTRAINT reports_reviewed_by_admin_id_fkey FOREIGN KEY (reviewed_by_admin_id) REFERENCES public.users(id) ON DELETE SET NULL -- If admin user deleted, keep report.
);

-- Logs various activities performed within the application.
CREATE TABLE public.activity_logs (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL, -- Primary key for the log entry.
  user_id uuid,                               -- Foreign key referencing the user who performed the action (if applicable).
  action_type text NOT NULL,                  -- Type of action performed (e.g., "user_login", "recipe_created").
  target_id text,                             -- ID of the entity targeted by the action (e.g., recipe ID, user ID).
  target_type text,                           -- Type of the target entity (e.g., "recipe", "user").
  details jsonb,                              -- Additional details about the action, stored as JSONB.
  ip_address inet,                            -- IP address from which the action was performed.
  user_agent text,                            -- User agent string of the client.
  created_at timestamp with time zone DEFAULT now(), -- Timestamp of when the activity occurred.
  CONSTRAINT activity_logs_pkey PRIMARY KEY (id),
  CONSTRAINT activity_logs_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL -- If user is deleted, keep logs but nullify user_id.
);

-- Stores records of users saving their favorite recipes.
CREATE TABLE public.saved_recipes (
  id uuid NOT NULL DEFAULT gen_random_uuid(), -- Primary key for the saved recipe entry.
  user_id uuid NOT NULL,                      -- Foreign key referencing the user who saved the recipe.
  recipe_id uuid NOT NULL,                    -- Foreign key referencing the recipe that was saved.
  saved_at timestamp with time zone DEFAULT now(), -- Timestamp of when the recipe was saved.
  CONSTRAINT saved_recipes_pkey PRIMARY KEY (id),
  CONSTRAINT saved_recipes_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE, -- If user is deleted, remove their saved recipes.
  CONSTRAINT saved_recipes_recipe_id_fkey FOREIGN KEY (recipe_id) REFERENCES public.recipes(id) ON DELETE CASCADE, -- If recipe is deleted, remove it from saved lists.
  CONSTRAINT saved_recipes_user_recipe_unique UNIQUE (user_id, recipe_id) -- Ensures a user can save a recipe only once.
);

-- Extends user information with additional profile details. Often linked 1-to-1 with `users`.
-- Note: The application seems to use this in conjunction with the `users` table rather than Supabase's `auth.users` and `public.profiles` convention directly for all fields.
CREATE TABLE public.user_profiles (
  id uuid NOT NULL,                           -- Primary key, references `users.id`.
  first_name text,                            -- User's first name (can duplicate/sync with `users` table or be primary source).
  last_name text,                             -- User's last name.
  username text UNIQUE,                       -- User's username (can duplicate/sync with `users` table).
  cooking_level text DEFAULT 'Beginner'::text, -- User's cooking skill level.
  favorite_cuisine text,                      -- User's favorite cuisine.
  location text,                              -- User's location.
  bio text,                                   -- User's biography.
  admin_verified boolean DEFAULT false,       -- Flag if admin status has been verified (potentially redundant if `users.is_admin` is authoritative).
  active boolean NOT NULL DEFAULT true,       -- Account active status (can duplicate/sync with `users.active`).
  last_login timestamp with time zone,        -- Timestamp of the user's last login.
  created_at timestamp with time zone DEFAULT now(), -- Timestamp of profile creation.
  updated_at timestamp with time zone DEFAULT now(), -- Timestamp of last profile update.
  CONSTRAINT user_profiles_pkey PRIMARY KEY (id),
  CONSTRAINT user_profiles_id_fkey FOREIGN KEY (id) REFERENCES public.users(id) ON DELETE CASCADE -- If user is deleted, delete their profile.
);

-- Tracks user's progress while cooking a specific recipe.
CREATE TABLE public.user_recipe_progress (
  id uuid NOT NULL DEFAULT gen_random_uuid(), -- Primary key for the progress entry.
  user_id uuid NOT NULL,                      -- Foreign key referencing the user.
  recipe_id uuid NOT NULL,                    -- Foreign key referencing the recipe.
  current_instruction_step integer DEFAULT 0, -- The last completed or current step number in the recipe instructions.
  checked_ingredients jsonb DEFAULT '[]'::jsonb, -- JSON array of ingredient indices that the user has marked as checked/prepared.
  completed_at timestamp with time zone,      -- Timestamp if the user marked the recipe as completed.
  created_at timestamp with time zone DEFAULT now(), -- Timestamp of when progress tracking started for this recipe.
  updated_at timestamp with time zone DEFAULT now(), -- Timestamp of the last update to this progress entry.
  CONSTRAINT user_recipe_progress_pkey PRIMARY KEY (id),
  CONSTRAINT user_recipe_progress_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE, -- If user deleted, delete their progress.
  CONSTRAINT user_recipe_progress_recipe_id_fkey FOREIGN KEY (recipe_id) REFERENCES public.recipes(id) ON DELETE CASCADE, -- If recipe deleted, delete progress.
  CONSTRAINT user_recipe_progress_user_recipe_unique UNIQUE (user_id, recipe_id) -- Ensures one progress entry per user per recipe.
);

-- Stores active user sessions, particularly for distinguishing admin sessions.
CREATE TABLE public.user_sessions (
  session_id text NOT NULL,                   -- Unique session identifier (e.g., a GUID).
  user_id uuid NOT NULL,                      -- Foreign key referencing the user associated with the session.
  is_admin boolean NOT NULL DEFAULT false,    -- Flag indicating if this is an administrator session.
  expires_at timestamp with time zone NOT NULL, -- Timestamp when the session expires.
  created_at timestamp with time zone DEFAULT now(), -- Timestamp of session creation.
  CONSTRAINT user_sessions_pkey PRIMARY KEY (session_id),
  CONSTRAINT user_sessions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE -- If user is deleted, their sessions are invalidated.
);

-- This table seems to be a duplicate or an alternative older version of user_profiles or users.
-- Based on the C# models, `public.users` and `public.user_profiles` are the main ones.
-- `profiles` linked to `auth.users` is a common Supabase pattern but the app uses `public.users`.
-- Removing this to avoid confusion unless it serves a distinct, identified purpose.
-- CREATE TABLE public.profiles (
--   id uuid NOT NULL,
--   username text UNIQUE,
--   first_name text,
--   last_name text,
--   cooking_level text,
--   is_admin boolean DEFAULT false,
--   CONSTRAINT profiles_pkey PRIMARY KEY (id),
--   CONSTRAINT profiles_id_fkey FOREIGN KEY (id) REFERENCES auth.users(id) -- This assumes auth.users is the primary, which contradicts other app models.
-- );

-- This table is not explicitly defined in the C# models but might be used by a feature not fully explored
-- or could be a legacy table. If not used, it can be removed.
-- CREATE TABLE public.category_searches (
--   id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
--   category text NOT NULL UNIQUE,
--   search_count integer DEFAULT 1,
--   last_searched_at timestamp with time zone DEFAULT now(),
--   created_at timestamp with time zone DEFAULT now(),
--   CONSTRAINT category_searches_pkey PRIMARY KEY (id)
-- );

-- General Notes:
-- - UUIDs are used for most primary keys (`gen_random_uuid()` or client-generated).
-- - Timestamps are `timestamp with time zone`.
-- - `ON DELETE` policies (CASCADE, SET NULL) are suggested based on common practice but should be verified against application requirements.
-- - The `total_time` in `recipes` is shown as a generated column, which is a clean way to handle it if the DB supports it well. The C# model might also calculate it.
-- - The application uses a custom `users` table in the `public` schema rather than relying solely on `auth.users` for all user attributes.
--   `user_profiles` seems to act as an extension or a synchronized table for additional, mutable profile data.