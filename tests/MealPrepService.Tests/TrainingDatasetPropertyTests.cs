using FsCheck;
using FsCheck.Xunit;
using FluentValidation;
using MealPrepService.BusinessLogicLayer.Validators;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.Tests;

/// <summary>
/// Property-based tests for TrainingDataset entity validation
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class TrainingDatasetPropertyTests
{
    private readonly TrainingDatasetValidator _validator;

    public TrainingDatasetPropertyTests()
    {
        _validator = new TrainingDatasetValidator();
    }

    /// <summary>
    /// Property 4: Invalid dataset rejection
    /// For any training dataset record with invalid data (missing required fields, invalid JSON, negative calorie targets),
    /// the validation should reject it and return descriptive error messages.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Property", "Property 4: Invalid dataset rejection")]
    public Property InvalidDatasetRejection_MissingRequiredFields()
    {
        return Prop.ForAll(
            GenerateInvalidTrainingDatasetWithMissingFields(),
            dataset =>
            {
                // Act: Validate the dataset
                var result = _validator.Validate(dataset);

                // Assert: Validation should fail with descriptive error messages
                return !result.IsValid
                    && result.Errors.Count > 0
                    && result.Errors.Any(e => !string.IsNullOrWhiteSpace(e.ErrorMessage));
            });
    }

    /// <summary>
    /// Property 4: Invalid dataset rejection - Invalid JSON
    /// For any training dataset record with invalid JSON in PreferredMealTypes or RecommendationWeights,
    /// the validation should reject it and return descriptive error messages.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Property", "Property 4: Invalid dataset rejection")]
    public Property InvalidDatasetRejection_InvalidJson()
    {
        return Prop.ForAll(
            GenerateInvalidTrainingDatasetWithInvalidJson(),
            dataset =>
            {
                // Act: Validate the dataset
                var result = _validator.Validate(dataset);

                // Assert: Validation should fail with descriptive error messages about JSON
                return !result.IsValid
                    && result.Errors.Count > 0
                    && result.Errors.Any(e => e.ErrorMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase));
            });
    }

    /// <summary>
    /// Property 4: Invalid dataset rejection - Negative Calorie Targets
    /// For any training dataset record with negative or zero calorie targets,
    /// the validation should reject it and return descriptive error messages.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Property", "Property 4: Invalid dataset rejection")]
    public Property InvalidDatasetRejection_NegativeCalorieTargets()
    {
        return Prop.ForAll(
            GenerateInvalidTrainingDatasetWithNegativeCalories(),
            dataset =>
            {
                // Act: Validate the dataset
                var result = _validator.Validate(dataset);

                // Assert: Validation should fail with descriptive error messages about calories
                return !result.IsValid
                    && result.Errors.Count > 0
                    && result.Errors.Any(e => 
                        e.PropertyName == nameof(TrainingDataset.AverageCalorieTarget)
                        && !string.IsNullOrWhiteSpace(e.ErrorMessage));
            });
    }

    /// <summary>
    /// Property 4: Invalid dataset rejection - Multiple Validation Errors
    /// For any training dataset record with multiple validation errors,
    /// the validation should reject it and return all descriptive error messages.
    /// **Validates: Requirements 1.5**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Property", "Property 4: Invalid dataset rejection")]
    public Property InvalidDatasetRejection_MultipleErrors()
    {
        return Prop.ForAll(
            GenerateInvalidTrainingDatasetWithMultipleErrors(),
            dataset =>
            {
                // Act: Validate the dataset
                var result = _validator.Validate(dataset);

                // Assert: Validation should fail with multiple descriptive error messages
                return !result.IsValid
                    && result.Errors.Count >= 2
                    && result.Errors.All(e => !string.IsNullOrWhiteSpace(e.ErrorMessage));
            });
    }

    #region Generators

    /// <summary>
    /// Generate TrainingDataset with missing required fields
    /// </summary>
    private static Arbitrary<TrainingDataset> GenerateInvalidTrainingDatasetWithMissingFields()
    {
        var gen = Gen.OneOf(
            // Missing CustomerSegment
            from mealTypes in GenerateValidJsonArray()
            from calories in Gen.Choose(100, 3000)
            from weights in GenerateValidJsonObject()
            select new TrainingDataset
            {
                CustomerSegment = string.Empty,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                RecommendationWeights = weights
            },
            // Missing PreferredMealTypes
            from segment in GenerateNonEmptyString(3, 50)
            from calories in Gen.Choose(100, 3000)
            from weights in GenerateValidJsonObject()
            select new TrainingDataset
            {
                CustomerSegment = segment,
                PreferredMealTypes = string.Empty,
                AverageCalorieTarget = calories,
                RecommendationWeights = weights
            },
            // Missing RecommendationWeights
            from segment in GenerateNonEmptyString(3, 50)
            from mealTypes in GenerateValidJsonArray()
            from calories in Gen.Choose(100, 3000)
            select new TrainingDataset
            {
                CustomerSegment = segment,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                RecommendationWeights = string.Empty
            }
        );

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate TrainingDataset with invalid JSON
    /// </summary>
    private static Arbitrary<TrainingDataset> GenerateInvalidTrainingDatasetWithInvalidJson()
    {
        var gen = Gen.OneOf(
            // Invalid JSON in PreferredMealTypes
            from segment in GenerateNonEmptyString(3, 50)
            from invalidJson in GenerateInvalidJson()
            from calories in Gen.Choose(100, 3000)
            from weights in GenerateValidJsonObject()
            select new TrainingDataset
            {
                CustomerSegment = segment,
                PreferredMealTypes = invalidJson,
                AverageCalorieTarget = calories,
                RecommendationWeights = weights
            },
            // Invalid JSON in RecommendationWeights
            from segment in GenerateNonEmptyString(3, 50)
            from mealTypes in GenerateValidJsonArray()
            from calories in Gen.Choose(100, 3000)
            from invalidJson in GenerateInvalidJson()
            select new TrainingDataset
            {
                CustomerSegment = segment,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                RecommendationWeights = invalidJson
            },
            // Invalid JSON in CommonAllergies
            from segment in GenerateNonEmptyString(3, 50)
            from mealTypes in GenerateValidJsonArray()
            from calories in Gen.Choose(100, 3000)
            from weights in GenerateValidJsonObject()
            from invalidJson in GenerateInvalidJson()
            select new TrainingDataset
            {
                CustomerSegment = segment,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                CommonAllergies = invalidJson,
                RecommendationWeights = weights
            }
        );

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate TrainingDataset with negative or zero calorie targets
    /// </summary>
    private static Arbitrary<TrainingDataset> GenerateInvalidTrainingDatasetWithNegativeCalories()
    {
        var gen = from segment in GenerateNonEmptyString(3, 50)
                  from mealTypes in GenerateValidJsonArray()
                  from calories in Gen.Choose(-1000, 0)
                  from weights in GenerateValidJsonObject()
                  select new TrainingDataset
                  {
                      CustomerSegment = segment,
                      PreferredMealTypes = mealTypes,
                      AverageCalorieTarget = calories,
                      RecommendationWeights = weights
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate TrainingDataset with multiple validation errors
    /// </summary>
    private static Arbitrary<TrainingDataset> GenerateInvalidTrainingDatasetWithMultipleErrors()
    {
        var gen = Gen.OneOf(
            // Missing fields + negative calories
            from invalidJson in GenerateInvalidJson()
            from calories in Gen.Choose(-1000, 0)
            select new TrainingDataset
            {
                CustomerSegment = string.Empty,
                PreferredMealTypes = invalidJson,
                AverageCalorieTarget = calories,
                RecommendationWeights = string.Empty
            },
            // Invalid JSON + negative calories
            from segment in GenerateNonEmptyString(3, 50)
            from invalidJson1 in GenerateInvalidJson()
            from calories in Gen.Choose(-500, 0)
            from invalidJson2 in GenerateInvalidJson()
            select new TrainingDataset
            {
                CustomerSegment = segment,
                PreferredMealTypes = invalidJson1,
                AverageCalorieTarget = calories,
                RecommendationWeights = invalidJson2
            }
        );

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate a valid JSON array string
    /// </summary>
    private static Gen<string> GenerateValidJsonArray()
    {
        return from count in Gen.Choose(1, 5)
               from items in Gen.ListOf(count, GenerateNonEmptyString(3, 20))
               let jsonItems = string.Join(",", items.Select(s => $"\"{s}\""))
               select $"[{jsonItems}]";
    }

    /// <summary>
    /// Generate a valid JSON object string
    /// </summary>
    private static Gen<string> GenerateValidJsonObject()
    {
        return from count in Gen.Choose(1, 5)
               from keys in Gen.ListOf(count, GenerateNonEmptyString(3, 20))
               from values in Gen.ListOf(count, Gen.Choose(1, 100))
               let jsonPairs = string.Join(",", keys.Zip(values, (k, v) => $"\"{k}\":{v}"))
               select $"{{{jsonPairs}}}";
    }

    /// <summary>
    /// Generate invalid JSON strings
    /// </summary>
    private static Gen<string> GenerateInvalidJson()
    {
        return Gen.OneOf(
            Gen.Constant("{invalid json}"),
            Gen.Constant("[unclosed array"),
            Gen.Constant("not json at all"),
            Gen.Constant("{\"key\": }"),
            Gen.Constant("[1, 2, 3,]"),
            Gen.Constant("{'single': 'quotes'}"),
            from text in GenerateNonEmptyString(5, 30)
            select text
        );
    }

    /// <summary>
    /// Generate a non-empty string with specified length range
    /// </summary>
    private static Gen<string> GenerateNonEmptyString(int minLength, int maxLength)
    {
        return from length in Gen.Choose(minLength, maxLength)
               from chars in Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()))
               let str = new string(chars).Trim()
               where !string.IsNullOrWhiteSpace(str) && str.Length >= minLength
               select str.Length > maxLength ? str.Substring(0, maxLength) : str;
    }

    #endregion
}
