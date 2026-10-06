namespace Memish_Tutor.Services.Common
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }
        public string? RawResponse { get; }

        private Result(bool isSuccess, T? value, string? error, string? rawResponse = null)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            RawResponse = rawResponse;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error, string? rawResponse = null) =>
            new(false, default, error, rawResponse);
    }
}
