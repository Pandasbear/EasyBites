# Recipe Management (`RecipesController.cs`)

This system handles all aspects of recipe creation, browsing, interaction, and image generation.

## Key Functionalities:

### For All Users (including anonymous):

*   **Get All Recipes (`GET /api/recipes`):**
    *   Retrieves a list of all *approved* and *non-draft* recipes.
    *   Supports filtering by search query, category, difficulty, and total cooking time.
*   **Search Recipes (`GET /api/recipes/search`):**
    *   Searches *approved* and *non-draft* recipes by name based on a query string.
*   **Filter Recipes (`GET /api/recipes/filter`):**
    *   Filters *approved* and *non-draft* recipes by difficulty and/or total cooking time.
*   **Get Recipe by ID (`GET /api/recipes/{id}`):**
    *   Retrieves a specific recipe by its ID.
    *   Enforces visibility:
        *   Approved and non-draft recipes are publicly visible.
        *   Drafts or non-approved recipes are only visible to their owner or an admin.
*   **Get Recipes by User ID (`GET /api/recipes/by-user/{userId}` & `GET /api/recipes/user/{userId}`):**
    *   Retrieves all recipes submitted by a specific user.
    *   Requires authentication; only the user themselves or an admin can access this.
*   **Generate Temporary Image (`POST /api/recipes/temp-image-gen`):**
    *   Allows anonymous users (e.g., during recipe submission before saving) to generate a temporary AI image based on recipe name and description.
    *   Returns a Base64 encoded image data URL.

### For Authenticated Users:

*   **Submit Recipe (`POST /api/recipes/submit`):**
    *   Allows authenticated users to submit a new recipe.
    *   The recipe is initially marked as `pending` (or `draft` if specified) and associated with the submitting user.
    *   Requires details like name, description, category, difficulty, times, ingredients, instructions, etc.
*   **Update Recipe (`PUT /api/recipes/{id}`):**
    *   Allows the recipe owner or an admin to update an existing recipe.
    *   Cannot change submission date directly through this endpoint. Status and draft status can be updated.
*   **Delete Recipe (`DELETE /api/recipes/{id}`):**
    *   Allows the recipe owner or an admin to delete a recipe.
    *   Also deletes associated `user_recipe_progress` and `saved_recipes` records.
*   **Save Recipe (`POST /api/recipes/saved`):**
    *   Allows users to save a recipe to their personal list. Records are stored in `saved_recipes`.
*   **Unsave Recipe (`DELETE /api/recipes/saved/{recipeId}`):**
    *   Allows users to remove a recipe from their saved list.
*   **Get Saved Recipes (`GET /api/recipes/saved`):**
    *   Retrieves all recipes saved by the currently authenticated user.
*   **Check if Recipe is Saved (`GET /api/recipes/saved/{recipeId}`):**
    *   Checks if a specific recipe is in the current user's saved list.
*   **Recipe Progress Tracking:**
    *   **Get Progress (`GET /api/recipes/progress/{recipeId}`):** Retrieves the user's current progress for a specific recipe.
    *   **Create/Update Progress (`POST /api/recipes/progress`, `PUT /api/recipes/progress/{id}`):** Creates or updates the user's cooking progress for a recipe (current step, checked ingredients). Stored in `user_recipe_progress`.
*   **Generate Recipe Image (`POST /api/recipes/{id}/generate-image`):**
    *   Allows the recipe owner or an admin to generate an AI image for a specific recipe using Gemini via `RecipeImageService`.
    *   The generated image URL is saved to the recipe record.
*   **Regenerate Recipe Image (`POST /api/recipes/{id}/regenerate-image`):**
    *   Allows the recipe owner or an admin to delete an existing AI-generated image (if any) and generate a new one.
*   **Publish Recipe (`PUT /api/recipes/{id}/publish`):**
    *   Allows the recipe owner or an admin to change a recipe's status from `draft` to `pending` (for admin review).
*   **Adjust Recipe Servings (`POST /api/recipes/{id}/adjust-servings`):**
    *   Allows users to dynamically adjust recipe serving sizes with AI-powered ingredient scaling.
    *   Creates recipe variations that are cached for improved performance.
*   **Recipe Variations Management:**
    *   **Create Variation (`POST /api/recipes/{id}/variations`):** Create a new recipe variation with modified ingredients and instructions.
    *   **Get Variations (`GET /api/recipes/{id}/variations`):** Retrieve all variations for a specific recipe.
    *   **Get Specific Variation (`GET /api/recipes/{id}/variations/{variationId}`):** Get details of a specific recipe variation.
    *   **Update Variation (`PUT /api/recipes/{id}/variations/{variationId}`):** Update an existing recipe variation.
    *   **Delete Variation (`DELETE /api/recipes/{id}/variations/{variationId}`):** Remove a recipe variation.

### For Admins Only (via `AdminController` but related to recipes):
    *   **Delete Recipe Image (`DELETE /api/recipes/{id}/image` - in `RecipesController` but admin-only):** Allows an admin to remove the image URL from a recipe record (doesn't delete from storage directly via this endpoint, but `RecipeImageService` handles actual deletion when regenerating).

## Supporting Models & Services:

*   `Recipe` (Model): Defines the structure of a recipe with fields for ingredients, instructions, timing, and metadata.
*   `RecipeVariance` (Model): Stores recipe variations with different serving sizes and modified ingredients/instructions.
*   `Rating` (Model): Stores user ratings for recipes (1-5 scale).
*   `SavedRecipe` (Model): Links users and their saved recipes.
*   `UserRecipeProgress` (Model): Tracks user's cooking progress including current step and checked ingredients.
*   `RecipeImageService`: Handles logic for generating, uploading (via `SupabaseStorageService`), and deleting recipe images using `GeminiService`.
*   `GeminiService`: Interacts with Google Gemini for AI image prompt generation, image creation, and recipe scaling.
*   `SupabaseStorageService`: Manages image file storage in Supabase storage buckets.
*   `ActivityLogService`: Logs all recipe-related activities for audit purposes.
*   Various DTOs for request/response data.

## Image Generation Flow:

1.  User (owner/admin) requests image generation for a recipe.
2.  `RecipesController` calls `RecipeImageService`.
3.  `RecipeImageService` uses `GeminiService` to:
    a.  Generate a detailed text prompt based on recipe details.
    b.  Generate an image from this prompt.
4.  If image data is received, `RecipeImageService` uses `SupabaseStorageService` to upload the image to the `recipe-images` bucket.
5.  The public URL of the uploaded image is returned and saved in the `recipes` table.
6.  For temporary image generation (during recipe submission), a base64 data URL is returned directly without uploading to storage.
