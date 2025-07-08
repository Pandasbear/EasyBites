using Supabase.Storage;

namespace EasyBites.Services;

public class SupabaseStorageService
{
    private readonly Supabase.Client _supabase;
    private readonly ILogger<SupabaseStorageService> _logger;
    private const string RECIPE_IMAGES_BUCKET = "recipe-images";

    public SupabaseStorageService(Supabase.Client supabase, ILogger<SupabaseStorageService> logger)
    {
        _supabase = supabase;
        _logger = logger;
    }

    public async Task<string?> UploadRecipeImageAsync(byte[] imageData, string recipeId, string fileExtension = "jpg")
    {
        try
        {
            // Create a unique filename
            var fileName = $"{recipeId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fileExtension}";
            var filePath = $"recipes/{fileName}";

            // Upload to Supabase Storage
            var storage = _supabase.Storage.From(RECIPE_IMAGES_BUCKET);
            
            // Upload the file
            await storage.Upload(imageData, filePath, new Supabase.Storage.FileOptions 
            { 
                ContentType = $"image/{fileExtension}",
                Upsert = true 
            });

            // Get the public URL
            var publicUrl = storage.GetPublicUrl(filePath);
            
            _logger.LogInformation("Successfully uploaded recipe image for recipe {RecipeId} to {FilePath}", recipeId, filePath);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload recipe image for recipe {RecipeId}", recipeId);
            return null;
        }
    }

    public async Task<bool> DeleteRecipeImageAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
                return true;

            // Extract the file path from the URL
            var uri = new Uri(imageUrl);
            var segments = uri.AbsolutePath.Split('/');
            if (segments.Length < 2)
                return false;

            var filePath = string.Join("/", segments.Skip(segments.Length - 2));

            var storage = _supabase.Storage.From(RECIPE_IMAGES_BUCKET);
            await storage.Remove(filePath);

            _logger.LogInformation("Successfully deleted recipe image at {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recipe image at {ImageUrl}", imageUrl);
            return false;
        }
    }

    public async Task<bool> EnsureBucketExistsAsync()
    {
        try
        {
            var buckets = await _supabase.Storage.ListBuckets();
            var bucketExists = buckets?.Any(b => b.Name == RECIPE_IMAGES_BUCKET) ?? false;

            if (!bucketExists)
            {
                await _supabase.Storage.CreateBucket(RECIPE_IMAGES_BUCKET, new Supabase.Storage.BucketUpsertOptions
                {
                    Public = true
                });
                _logger.LogInformation("Created recipe-images bucket in Supabase Storage");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket exists");
            return false;
        }
    }
} 