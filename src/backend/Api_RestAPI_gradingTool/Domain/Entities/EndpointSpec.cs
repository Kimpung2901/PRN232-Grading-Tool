using System;
using System.Collections.Generic;

namespace Infrastructure;

public partial class EndpointSpec
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public string Method { get; set; } = null!;

    public string Url { get; set; } = null!;

    public int ExpectedStatus { get; set; }

    public string? RequestExample { get; set; }

    public string? ResponseSchema { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;
}
