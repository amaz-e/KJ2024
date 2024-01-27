namespace MemeBE.Models;

public class Result
{
    public bool Sucess { get; set; }
    public string Message { get; set; }

    public static Result Ok(string message = "Success")
    {
        return new Result() { Sucess = true, Message = message };
    }

    public static Result Fail(string message = "Fail")
    {
        return new Result() { Sucess = false, Message = message };
    }
}