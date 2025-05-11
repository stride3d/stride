namespace Stride.GameStudio.Git
{
    public class GitResult<T>
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public T? Data { get; private set; }
        public static GitResult<T> Ok(T data) => new GitResult<T> { Success = true, Data = data };
        public static GitResult<T> Fail(string error) => new GitResult<T> { Success = false, ErrorMessage = error };
    }
}
