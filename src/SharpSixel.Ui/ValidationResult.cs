using System;

namespace SharpSixel.Ui;

/// <summary>Outcome of validating a prompt input: valid, or invalid with a message shown to the user.</summary>
public sealed class ValidationResult
{
    private static readonly ValidationResult Valid = new ValidationResult(null);

    private ValidationResult(string? message)
    {
        Message = message;
    }

    /// <summary>True if the input is accepted.</summary>
    public bool IsValid => Message == null;

    /// <summary>Error message of an invalid result; null if valid.</summary>
    public string? Message { get; }

    /// <summary>The input is accepted.</summary>
    public static ValidationResult Success() => Valid;

    /// <summary>The input is rejected; <paramref name="message"/> is shown below the prompt.</summary>
    public static ValidationResult Error(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("The message must not be empty.", nameof(message));
        }

        return new ValidationResult(message);
    }
}
