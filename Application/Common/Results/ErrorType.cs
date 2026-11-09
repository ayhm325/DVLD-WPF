public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden,
    Failure,

    /// <summary>
    /// The caller is not authenticated (as opposed to authenticated but not allowed).
    /// Maps to HTTP 401.
    /// </summary>
    Unauthorized
}