namespace DVLD.Contracts.TestWorkflow;

public sealed record TestWorkflowResponse(
    bool Allowed,
    string? Error,
    string? ErrorType,
    int? NextTestType);