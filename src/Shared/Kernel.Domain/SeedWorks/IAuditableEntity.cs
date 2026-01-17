namespace Product.Template.Kernel.Domain.SeedWorks;

/// <summary>
/// Interface para entidades auditáveis
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Data e hora de criação da entidade
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Usuário que criou a entidade
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Data e hora da última atualização
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Usuário que realizou a última atualização
    /// </summary>
    string? UpdatedBy { get; set; }
}

