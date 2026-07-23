using SpriteRigStudio.Domain.Projects;

namespace SpriteRigStudio.Domain.Common;

/// <summary>
/// Represents the result of an operation that can fail.
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; protected set; }
    public string? ErrorCode { get; protected set; }

    protected Result() { IsSuccess = true; }

    protected Result(string errorCode, string errorMessage)
    {
        IsSuccess = false;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new();
    public static Result Failure(string code, string message) => new(code, message);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string code, string message) => Result<T>.Failure(code, message);
}

/// <summary>
/// Represents the result of an operation that returns a value or fails.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(string code, string message) => new(code, message);
}

/// <summary>
/// Result for project open operations.
/// </summary>
public class OpenProjectResult : Result
{
    public SpriteRigProject? Project { get; private set; }
    public List<string> Warnings { get; init; } = new();
    public List<string> MissingAssets { get; init; } = new();

    private OpenProjectResult(SpriteRigProject project)
    {
        IsSuccess = true;
        Project = project;
    }

    private OpenProjectResult(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    public static OpenProjectResult Success(SpriteRigProject project) => new(project);
    public new static OpenProjectResult Failure(string code, string message) => new(code, message);
}

/// <summary>
/// Result for save operations.
/// </summary>
public class SaveProjectResult : Result
{
    public string? FilePath { get; private set; }

    private SaveProjectResult(string filePath)
    {
        IsSuccess = true;
        FilePath = filePath;
    }

    private SaveProjectResult(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    public static SaveProjectResult Success(string filePath) => new(filePath);
    public new static SaveProjectResult Failure(string code, string message) => new(code, message);
}

/// <summary>
/// Result for export operations.
/// </summary>
public class ExportResult : Result
{
    public int FramesExported { get; private set; }
    public int WarningsCount { get; private set; }
    public string? OutputPath { get; private set; }

    private ExportResult(string outputPath, int frames, int warnings)
    {
        IsSuccess = true;
        OutputPath = outputPath;
        FramesExported = frames;
        WarningsCount = warnings;
    }

    private ExportResult(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    public static ExportResult Success(string outputPath, int frames, int warnings = 0) =>
        new(outputPath, frames, warnings);
    public new static ExportResult Failure(string code, string message) => new(code, message);
}

/// <summary>
/// Result for image import operations.
/// </summary>
public class ImportImageResult : Result
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string? StoredPath { get; private set; }

    private ImportImageResult(int width, int height, string storedPath)
    {
        IsSuccess = true;
        Width = width;
        Height = height;
        StoredPath = storedPath;
    }

    private ImportImageResult(string errorCode, string errorMessage) : base(errorCode, errorMessage) { }

    public static ImportImageResult Success(int width, int height, string storedPath) =>
        new(width, height, storedPath);
    public new static ImportImageResult Failure(string code, string message) => new(code, message);
}
