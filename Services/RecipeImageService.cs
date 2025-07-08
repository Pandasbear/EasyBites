namespace EasyBites.Services;

using System;

public class RecipeImageService
{
    private readonly Lazy<GeminiService> _geminiServiceLazy;
    private readonly SupabaseStorageService _storageService;
    private readonly ILogger<RecipeImageService> _logger;

    public RecipeImageService(
        Lazy<GeminiService> geminiService, 
        SupabaseStorageService storageService,
        ILogger<RecipeImageService> logger)
    {
        _geminiServiceLazy = geminiService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<ImageGenerationResult> GenerateTemporaryImageUrlAsync(string recipeName, string description)
    {
        try
        {
            _logger.LogInformation("Generating temporary image for recipe: {RecipeName}", recipeName);

            // Generate a generic prompt as full details aren't available yet
            // Using default values for category, difficulty, ingredients for prompt generation
            var imagePrompt = await _geminiServiceLazy.Value.GenerateImagePromptForRecipe(
                recipeName, description, "dish", "easy", new List<string>());

            _logger.LogInformation("Generated temporary image prompt: {Prompt}", imagePrompt);

            var imageData = await _geminiServiceLazy.Value.GenerateImageFromPrompt(imagePrompt);

            if (imageData == null || imageData.Length == 0)
            {
                _logger.LogWarning("No image data returned from Gemini for temporary image.");
                return new ImageGenerationResult
                {
                    Success = false,
                    ErrorMessage = "AI image generation is currently unavailable or returned no image.",
                    Prompt = imagePrompt
                };
            }

            // Convert image data to a Base64 string for direct return to frontend
            // Frontend will display this as a data URL (e.g., data:image/jpeg;base64,...)
            var base64Image = Convert.ToBase64String(imageData);
            var dataUrl = $"data:image/jpeg;base64,{base64Image}"; 

            _logger.LogInformation("Generated temporary Base64 image URL for {RecipeName}", recipeName);

            return new ImageGenerationResult
            {
                Success = true,
                ImageUrl = dataUrl,
                Prompt = imagePrompt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate temporary image for recipe {RecipeName}", recipeName);

            var errorMessage = ex.Message.Contains("credentials") || ex.Message.Contains("Authentication") ?
                "AI image generation requires Google Cloud credentials to be configured. Please check the setup guide." :
                $"Failed to generate temporary image: {ex.Message}";

            return new ImageGenerationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public async Task<ImageGenerationResult> GenerateAndUploadRecipeImageAsync(
        string recipeId,
        string recipeName, 
        string description, 
        string category, 
        string difficulty, 
        List<string> ingredients)
    {
        try
        {
            _logger.LogInformation("Starting image generation for recipe {RecipeId}: {RecipeName}", recipeId, recipeName);

            // Step 1: Generate detailed prompt using Gemini
            var imagePrompt = await _geminiServiceLazy.Value.GenerateImagePromptForRecipe(
                recipeName, description, category, difficulty, ingredients);

            _logger.LogInformation("Generated image prompt for recipe {RecipeId}: {Prompt}", recipeId, imagePrompt);

            // Step 2: Generate image using Gemini
            var imageData = await _geminiServiceLazy.Value.GenerateImageFromPrompt(imagePrompt);

            if (imageData == null)
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    ErrorMessage = "AI image generation is currently unavailable. Please ensure Google Cloud credentials are properly configured.",
                    Prompt = imagePrompt
                };
            }

            _logger.LogInformation("Successfully generated image for recipe {RecipeId}, size: {Size} bytes", recipeId, imageData.Length);

            // Step 3: Upload to Supabase Storage
            var imageUrl = await _storageService.UploadRecipeImageAsync(imageData, recipeId, "jpg");

            if (string.IsNullOrEmpty(imageUrl))
            {
                return new ImageGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to upload image to storage",
                    Prompt = imagePrompt
                };
            }

            _logger.LogInformation("Successfully uploaded image for recipe {RecipeId} to {ImageUrl}", recipeId, imageUrl);

            return new ImageGenerationResult
            {
                Success = true,
                ImageUrl = imageUrl,
                Prompt = imagePrompt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate and upload image for recipe {RecipeId}", recipeId);
            
            var errorMessage = ex.Message.Contains("credentials") || ex.Message.Contains("Authentication") ?
                "AI image generation requires Google Cloud credentials to be configured. Please check the setup guide." :
                $"Failed to generate image: {ex.Message}";
            
            return new ImageGenerationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    public async Task<bool> DeleteRecipeImageAsync(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return true;

        return await _storageService.DeleteRecipeImageAsync(imageUrl);
    }

    public async Task<ImageGenerationResult> RegenerateRecipeImageAsync(
        string recipeId,
        string recipeName, 
        string description, 
        string category, 
        string difficulty, 
        List<string> ingredients,
        string? existingImageUrl = null)
    {
        try
        {
            // Delete existing image if provided
            if (!string.IsNullOrEmpty(existingImageUrl))
            {
                await DeleteRecipeImageAsync(existingImageUrl);
            }

            // Generate new image
            return await GenerateAndUploadRecipeImageAsync(
                recipeId, recipeName, description, category, difficulty, ingredients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate image for recipe {RecipeId}", recipeId);
            
            var errorMessage = ex.Message.Contains("credentials") || ex.Message.Contains("Authentication") ?
                "AI image generation requires Google Cloud credentials to be configured. Please check the setup guide." :
                $"Failed to regenerate image: {ex.Message}";
            
            return new ImageGenerationResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
} 