namespace VenteApp;

public class User
{
    public Guid Id { get; set; } // Primary key, unique identifier for each user

    public string Nom { get; set; } // Required: Last name of the user
    public string Prenom { get; set; } // Required: First name of the user
    public string Numero { get; set; } // Required: Phone number
    public string Adresse { get; set; } // Optional: Address of the user
    public string Email { get; set; } // Required: Email address (validated for proper email format)

    public string Login { get; set; } // Required: Username for login
    public string Password { get; set; } // Required: Password for login (should be hashed before saving)

    public DateTime CreatedAt { get; set; } // Optional: Date when the user account was created
    public DateTime UpdatedAt { get; set; } // Optional: Date when the user account was last updated

    // Optionally, additional fields for user roles, permissions, etc.
    public string Role { get; set; } // Optional: Role of the user (e.g., "Admin", "User", etc.)
  
}
