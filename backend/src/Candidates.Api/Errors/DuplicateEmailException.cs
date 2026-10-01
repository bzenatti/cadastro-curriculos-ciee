namespace Candidates.Api.Errors;

public class DuplicateEmailException() : Exception("Já existe um candidato cadastrado com este e-mail.");
