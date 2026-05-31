using System;
using System.Collections.Generic;

namespace Umbral.TriviaGame.Domain.Common;

/// <summary>
/// Clase base abstracta para todas las entidades del dominio.
/// Proporciona igualdad por identidad (comparando el Id, no todos los campos).
/// </summary>
/// <typeparam name="TId">Tipo del identificador de la entidad (debe ser un value object no nulo).</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Identificador único de la entidad. Se asigna en construcción y no cambia durante el ciclo de vida.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Constructor protegido: solo las entidades concretas (o sus factory methods) pueden instanciar.
    /// </summary>
    /// <param name="id">Identificador único ya validado por el value object correspondiente.</param>
    protected Entity(TId id)
    {
        // Rechaza identificadores nulos como defensa ante errores de integración.
        Id = id ?? throw new ArgumentNullException(nameof(id), "El identificador de la entidad es obligatorio.");
    }

    #region Equality por identidad

    /// <summary>
    /// Compara si esta entidad es igual a otro objeto.
    /// Dos entidades son iguales si tienen el mismo tipo y el mismo Id.
    /// </summary>
    public override bool Equals(object? obj)
    {
        // Si el otro objeto es nulo o de distinto tipo, no son iguales.
        if (obj is null || obj.GetType() != GetType())
            return false;

        return Equals((Entity<TId>)obj);
    }

    /// <summary>
    /// Compara si esta entidad es igual a otra entidad del mismo tipo por su Id.
    /// </summary>
    public bool Equals(Entity<TId>? other)
    {
        // Entidad nula nunca es igual.
        if (other is null)
            return false;

        // Misma instancia en memoria es igual (referencia).
        if (ReferenceEquals(this, other))
            return true;

        // Comparación por identificador: dos entidades distintas pero con el mismo Id se consideran iguales.
        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Código hash basado únicamente en el Id.
    /// Garantiza consistencia con la igualdad por identidad.
    /// </summary>
    public override int GetHashCode()
    {
        // Usa el código hash del value object del Id.
        return Id.GetHashCode();
    }

    /// <summary>
    /// Operador de igualdad: delega a Equals.
    /// </summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Operador de desigualdad: negación del operador de igualdad.
    /// </summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    #endregion
}
