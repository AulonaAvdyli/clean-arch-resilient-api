namespace BackendAssignment.Application.DTOs;
public class ErrorResponse
{
    public string Message { get; set; }
    public string Type { get; set; } = "Error";
    public int Status { get; set; }

    public ErrorResponse(string message, int status)
    {
        Message = message;
        Status = status;
    }
}


