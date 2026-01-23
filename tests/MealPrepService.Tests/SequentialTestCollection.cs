using Xunit;

namespace MealPrepService.Tests;

/// <summary>
/// Collection definition for tests that must run sequentially
/// (e.g., tests that manipulate Console.Out)
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialTestCollection
{
}
