using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using System.Text.Json;
using ProtobufValue = Google.Protobuf.WellKnownTypes.Value;
using ProtobufStruct = Google.Protobuf.WellKnownTypes.Struct;
using System.IO;
using System.Linq;

namespace EasyBites.Services;

public class GeminiService
{
    private readonly string _projectId;
    private readonly string _location;
    private readonly string? _credentialsPath;
    private readonly ILogger<GeminiService> _logger;
    private readonly bool _isConfigured;
    private PredictionServiceClient? _predictionClient;
    private bool _clientInitialized = false;
    private readonly object _clientLock = new object();

    public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
    {
        _logger = logger;
        
        // Only read configuration, don't create any Google Cloud resources
        _projectId = configuration["Google:ProjectId"] ?? "";
        _location = configuration["Google:Location"] ?? "us-central1";

        // Optional explicit credentials path
        _credentialsPath = configuration["Google:CredentialsPath"];

        // If not supplied, automatically look for a *.json file in Secrets/
        if (string.IsNullOrEmpty(_credentialsPath))
        {
            try
            {
                string[] candidateDirs = {
                    Path.Combine(AppContext.BaseDirectory, "Secrets"), 
                    Path.Combine(Directory.GetCurrentDirectory(), "Secrets") 
                };

                foreach (var dir in candidateDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        var json = Directory.EnumerateFiles(dir, "*.json").FirstOrDefault();
                        if (json != null)
                        {
                            _credentialsPath = json;
                            _logger.LogInformation("Using Google credentials file found at {Path}", _credentialsPath);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not scan Secrets folder for credentials file");
            }
        }
        _isConfigured = !string.IsNullOrEmpty(_projectId);
        
        if (!_isConfigured)
        {
            _logger.LogWarning("Google Cloud ProjectId not configured. Image generation will be disabled.");
        }
        else
        {
            _logger.LogInformation("GeminiService initialized with project: {ProjectId}", _projectId);
        }
    }

    private PredictionServiceClient? GetPredictionClient()
    {
        if (!_isConfigured)
        {
            return null;
        }

        if (_clientInitialized)
        {
            return _predictionClient;
        }

        lock (_clientLock)
        {
            if (_clientInitialized)
            {
                return _predictionClient;
            }

            try
            {
                _logger.LogInformation("Creating Google Cloud PredictionServiceClient for project: {ProjectId}", _projectId);

                var builder = new PredictionServiceClientBuilder();
                if (!string.IsNullOrEmpty(_credentialsPath) && File.Exists(_credentialsPath))
                {
                    builder.CredentialsPath = _credentialsPath;
                    _logger.LogInformation("Credentials loaded from {Path}", _credentialsPath);
                }

                _predictionClient = builder.Build();
                _logger.LogInformation("Google Cloud PredictionServiceClient created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Google Cloud PredictionServiceClient. " +
                    "Image generation will be disabled. Please ensure Google Cloud Application Default Credentials are set up. " +
                    "See: https://cloud.google.com/docs/authentication/external/set-up-adc");
                _predictionClient = null;
            }
            
            _clientInitialized = true;
            return _predictionClient;
        }
    }

    private bool IsAvailable()
    {
        return _isConfigured && GetPredictionClient() != null;
    }

    public bool IsConfiguredAndAvailable()
    {
        return IsAvailable();
    }

    public async Task<string> GenerateImagePromptForRecipe(string recipeName, string description, string category, string difficulty, List<string> ingredients)
    {
        try
        {
            if (!IsAvailable())
            {
                _logger.LogWarning("Gemini service not available, returning fallback prompt for recipe {RecipeName}", recipeName);
                return GenerateFallbackPrompt(recipeName, category, ingredients);
            }

            var promptRequest = $@"
Create a detailed, vivid image prompt for a food photography shot of '{recipeName}'. 

Recipe Details:
- Description: {description}
- Category: {category}
- Difficulty: {difficulty}
- Key Ingredients: {string.Join(", ", ingredients.Take(5))}

Create a professional food photography prompt that includes:
1. The finished dish presentation
2. Lighting and composition details
3. Background and styling elements
4. Color palette and textures
5. Professional photography terms

Make it detailed enough for AI image generation, focusing on appetizing visual elements.
Keep it under 200 words and format as a single paragraph.
";

            var textPrompt = await GenerateTextWithGemini(promptRequest);
            return textPrompt ?? GenerateFallbackPrompt(recipeName, category, ingredients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image prompt for recipe {RecipeName}", recipeName);
            return GenerateFallbackPrompt(recipeName, category, ingredients);
        }
    }

    public async Task<byte[]?> GenerateImageFromPrompt(string prompt)
    {
        try
        {
            var client = GetPredictionClient();
            if (client == null)
            {
                _logger.LogWarning("Gemini service not available, cannot generate image");
                return null;
            }

            var endpoint = EndpointName.FromProjectLocationPublisherModel(_projectId, _location, "google", "imagen-3.0-generate-001");

            // Create instance value for Imagen
            var instanceStruct = new ProtobufStruct();
            instanceStruct.Fields.Add("prompt", ProtobufValue.ForString($"Professional food photography: {prompt}. High quality, appetizing, well-lit, detailed, 4K resolution."));
            var instanceValue = ProtobufValue.ForStruct(instanceStruct);

            // Create parameters value
            var parametersStruct = new ProtobufStruct();
            parametersStruct.Fields.Add("sampleCount", ProtobufValue.ForNumber(1));
            parametersStruct.Fields.Add("aspectRatio", ProtobufValue.ForString("1:1"));
            parametersStruct.Fields.Add("safetyFilterLevel", ProtobufValue.ForString("block_some"));
            parametersStruct.Fields.Add("personGeneration", ProtobufValue.ForString("dont_allow"));
            var parametersValue = ProtobufValue.ForStruct(parametersStruct);

            var request = new PredictRequest
            {
                EndpointAsEndpointName = endpoint,
                Instances = { instanceValue },
                Parameters = parametersValue
            };

            var response = await client.PredictAsync(request);
            
            if (response.Predictions.Count > 0)
            {
                var prediction = response.Predictions[0];
                if (prediction.StructValue.Fields.ContainsKey("bytesBase64Encoded"))
                {
                    var base64String = prediction.StructValue.Fields["bytesBase64Encoded"].StringValue;
                    return Convert.FromBase64String(base64String);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image with prompt: {Prompt}", prompt);
            return null;
        }
    }

    private async Task<string?> GenerateTextWithGemini(string prompt)
    {
        try
        {
            var client = GetPredictionClient();
            if (client == null)
            {
                return null;
            }

            var endpoint = EndpointName.FromProjectLocationPublisherModel(_projectId, _location, "google", "gemini-1.5-pro");

            // Create instance for text generation
            var instanceStruct = new ProtobufStruct();
            instanceStruct.Fields.Add("prompt", ProtobufValue.ForString(prompt));
            var instanceValue = ProtobufValue.ForStruct(instanceStruct);

            // Create parameters for text generation
            var parametersStruct = new ProtobufStruct();
            parametersStruct.Fields.Add("temperature", ProtobufValue.ForNumber(0.7));
            parametersStruct.Fields.Add("maxOutputTokens", ProtobufValue.ForNumber(256));
            parametersStruct.Fields.Add("topK", ProtobufValue.ForNumber(40));
            parametersStruct.Fields.Add("topP", ProtobufValue.ForNumber(0.95));
            var parametersValue = ProtobufValue.ForStruct(parametersStruct);

            var request = new PredictRequest
            {
                EndpointAsEndpointName = endpoint,
                Instances = { instanceValue },
                Parameters = parametersValue
            };

            var response = await client.PredictAsync(request);
            
            if (response.Predictions.Count > 0)
            {
                var prediction = response.Predictions[0];
                if (prediction.StructValue.Fields.ContainsKey("content"))
                {
                    return prediction.StructValue.Fields["content"].StringValue;
                }
                else if (prediction.StructValue.Fields.ContainsKey("candidates"))
                {
                    var candidates = prediction.StructValue.Fields["candidates"].ListValue;
                    if (candidates.Values.Count > 0)
                    {
                        var firstCandidate = candidates.Values[0].StructValue;
                        if (firstCandidate.Fields.ContainsKey("content"))
                        {
                            return firstCandidate.Fields["content"].StringValue;
                        }
                    }
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate text with Gemini: {Message}", ex.Message);
            return null;
        }
    }

    private string GenerateFallbackPrompt(string recipeName, string category, List<string> ingredients)
    {
        var ingredientList = string.Join(", ", ingredients.Take(3));
        return $"Professional food photography of {recipeName}, a delicious {category.ToLower()} dish featuring {ingredientList}, beautifully plated, warm natural lighting, rustic wooden background, appetizing presentation, high resolution, detailed textures, restaurant quality";
    }
}

public class ImageGenerationResult
{
    public bool Success { get; set; }
    public string? ImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Prompt { get; set; }
} 