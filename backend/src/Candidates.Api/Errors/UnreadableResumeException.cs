namespace Candidates.Api.Errors;

public class UnreadableResumeException(string message) : Exception(message);
