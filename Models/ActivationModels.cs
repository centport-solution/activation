namespace ActivationAPI.Models;

public class GenerateRequest {
    public string MachineKey { get; set; } = "";
    public int DurationDays { get; set; }
}

public class ActivateRequest {
    public string MachineKey { get; set; } = "";
    public string ActivationCode { get; set; } = "";
}

public class ValidateRequest {
    public string MachineKey { get; set; } = "";
    public string ActivationCode { get; set; } = "";
}

public class ActivationRecord {
    public string ActivationCode { get; set; } = "";
    public string MachineKey { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DurationDays { get; set; }
    public bool IsUsed { get; set; }
}