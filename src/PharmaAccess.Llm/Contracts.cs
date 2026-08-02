namespace PharmaAccess.Llm;

public interface ILanguageModelClient
{
    string Provider { get; }
    string Model { get; }
    bool IsAvailable { get; }
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}

public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string question, int limit = 5, CancellationToken cancellationToken = default);
}

public interface ILlmResponseValidator
{
    ValidationResult Validate(LlmStructuredResponse response, IReadOnlyList<RetrievedChunk> evidence);
}

public interface IDocumentIngestionService
{
    Task<IReadOnlyList<ResearchChunk>> IngestAsync(CancellationToken cancellationToken = default);
}

public sealed record ResearchDocument(string Identifier, string Title, string SourcePath, string Text, string TrustLevel = "verified-public");
public sealed record ResearchChunk(string DocumentIdentifier, string Title, string SourcePath, string Section, int ChunkIndex, string Text, string Sha256, string TrustLevel);
public sealed record RetrievedChunk(ResearchChunk Chunk, double Score);
public sealed record NumericalClaim(string Value, string Description, string SourceIdentifier);
public sealed record LlmStructuredResponse(string Answer, string[] Citations, NumericalClaim[] NumericalClaims, string CausalLanguageClassification, string[] Warnings, string ConfidenceSupportStatus);
public sealed record ValidationResult(bool IsValid, string Status, string[] Errors);
public sealed record AssistantAskRequest(string Question);
public sealed record AssistantCitation(string SourceIdentifier, string Title, string SourcePath, string Section, int ChunkIndex, string Sha256);
public sealed record AssistantAskResponse(string Answer, AssistantCitation[] Citations, string[] Warnings, string ValidationStatus, string Provider, string Model, string ExplanationType);
