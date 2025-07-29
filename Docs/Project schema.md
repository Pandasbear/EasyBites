-- WARNING: This schema is for context only and is not meant to be run.
-- Table order and constraints may not be valid for execution.

CREATE TABLE public.activity_logs (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  user_id uuid,
  action_type text NOT NULL,
  target_id text,
  target_type text,
  details jsonb,
  ip_address inet,
  user_agent text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT activity_logs_pkey PRIMARY KEY (id),
  CONSTRAINT activity_logs_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id)
);
CREATE TABLE public.category_searches (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  category text NOT NULL UNIQUE,
  search_count integer DEFAULT 1,
  last_searched_at timestamp with time zone DEFAULT now(),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT category_searches_pkey PRIMARY KEY (id)
);
CREATE TABLE public.feedback (
  user_id uuid,
  reviewed_at timestamp with time zone,
  reviewed_by_admin_id text,
  admin_response text,
  name text,
  email text,
  type text NOT NULL,
  subject text NOT NULL,
  message text NOT NULL,
  rating integer,
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  submitted_at timestamp with time zone DEFAULT now(),
  status text DEFAULT 'new'::text,
  CONSTRAINT feedback_pkey PRIMARY KEY (id),
  CONSTRAINT feedback_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id)
);
CREATE TABLE public.profiles (
  is_admin boolean DEFAULT false,
  id uuid NOT NULL,
  username text UNIQUE,
  first_name text,
  last_name text,
  cooking_level text,
  CONSTRAINT profiles_pkey PRIMARY KEY (id),
  CONSTRAINT profiles_id_fkey FOREIGN KEY (id) REFERENCES auth.users(id)
);
CREATE TABLE public.ratings (
  recipe_id uuid NOT NULL,
  user_id uuid NOT NULL,
  score integer NOT NULL CHECK (score >= 1 AND score <= 5),
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT ratings_pkey PRIMARY KEY (id),
  CONSTRAINT ratings_recipe_id_fkey FOREIGN KEY (recipe_id) REFERENCES public.recipes(id),
  CONSTRAINT ratings_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id)
);
CREATE TABLE public.recipe_variations (
  original_recipe_id uuid NOT NULL,
  user_id uuid NOT NULL,
  variation_name character varying NOT NULL,
  description text,
  modified_ingredients ARRAY NOT NULL,
  modified_instructions ARRAY,
  notes text,
  updated_at timestamp with time zone,
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT recipe_variations_pkey PRIMARY KEY (id),
  CONSTRAINT fk_recipe_variations_original_recipe FOREIGN KEY (original_recipe_id) REFERENCES public.recipes(id),
  CONSTRAINT fk_recipe_variations_user FOREIGN KEY (user_id) REFERENCES public.users(id)
);
CREATE TABLE public.recipes (
  is_draft boolean NOT NULL DEFAULT false,
  name text NOT NULL,
  description text,
  category text,
  difficulty text,
  prep_time integer,
  cook_time integer,
  servings integer,
  tips text,
  nutrition_info text,
  author text,
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  submitted_at timestamp with time zone DEFAULT now(),
  status text DEFAULT 'pending'::text,
  total_time integer DEFAULT (prep_time + cook_time),
  ingredients ARRAY,
  instructions ARRAY,
  dietary_options ARRAY,
  user_id text,
  image_url text,
  CONSTRAINT recipes_pkey PRIMARY KEY (id)
);
CREATE TABLE public.reports (
  id bigint GENERATED ALWAYS AS IDENTITY NOT NULL,
  reporter_user_id uuid,
  reported_user_id uuid,
  reported_recipe_id uuid,
  report_type text NOT NULL,
  description text NOT NULL,
  reviewed_at timestamp with time zone,
  reviewed_by_admin_id uuid,
  admin_notes text,
  status text DEFAULT 'pending'::text,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT reports_pkey PRIMARY KEY (id),
  CONSTRAINT reports_reported_user_id_fkey FOREIGN KEY (reported_user_id) REFERENCES public.users(id),
  CONSTRAINT reports_reported_recipe_id_fkey FOREIGN KEY (reported_recipe_id) REFERENCES public.recipes(id),
  CONSTRAINT reports_reporter_user_id_fkey FOREIGN KEY (reporter_user_id) REFERENCES public.users(id),
  CONSTRAINT reports_reviewed_by_admin_id_fkey FOREIGN KEY (reviewed_by_admin_id) REFERENCES public.users(id)
);
CREATE TABLE public.saved_recipes (
  user_id text NOT NULL,
  recipe_id text NOT NULL,
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  saved_at timestamp with time zone DEFAULT now(),
  CONSTRAINT saved_recipes_pkey PRIMARY KEY (id)
);
CREATE TABLE public.user_profiles (
  active boolean NOT NULL DEFAULT true,
  id uuid NOT NULL,
  last_login timestamp with time zone,
  first_name text,
  last_name text,
  username text,
  favorite_cuisine text,
  location text,
  bio text,
  admin_verified boolean DEFAULT false,
  account_status text DEFAULT 'active'::text,
  cooking_level text DEFAULT 'Beginner'::text,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  CONSTRAINT user_profiles_pkey PRIMARY KEY (id),
  CONSTRAINT user_profiles_id_fkey FOREIGN KEY (id) REFERENCES public.users(id)
);
CREATE TABLE public.user_recipe_progress (
  user_id uuid NOT NULL,
  recipe_id uuid NOT NULL,
  completed_at timestamp with time zone,
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  current_instruction_step integer DEFAULT 0,
  checked_ingredients jsonb DEFAULT '[]'::jsonb,
  updated_at timestamp with time zone DEFAULT now(),
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT user_recipe_progress_pkey PRIMARY KEY (id),
  CONSTRAINT user_recipe_progress_recipe_id_fkey FOREIGN KEY (recipe_id) REFERENCES public.recipes(id),
  CONSTRAINT user_recipe_progress_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id)
);
CREATE TABLE public.user_sessions (
  session_id text NOT NULL,
  user_id uuid NOT NULL,
  expires_at timestamp with time zone NOT NULL,
  is_admin boolean NOT NULL DEFAULT false,
  created_at timestamp with time zone DEFAULT now(),
  CONSTRAINT user_sessions_pkey PRIMARY KEY (session_id),
  CONSTRAINT user_sessions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id)
);
CREATE TABLE public.users (
  active boolean NOT NULL DEFAULT true,
  first_name text,
  last_name text,
  email text NOT NULL UNIQUE,
  username text NOT NULL UNIQUE,
  password_hash text NOT NULL,
  cooking_level text,
  id uuid NOT NULL DEFAULT gen_random_uuid(),
  is_admin boolean DEFAULT false,
  bio text,
  favorite_cuisine text,
  location text,
  profile_image_url text,
  created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
  admin_security_code text,
  CONSTRAINT users_pkey PRIMARY KEY (id)
);