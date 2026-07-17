using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class InvitacionEquipoRepository : IInvitacionEquipoRepository
{
    private readonly IdentityDbContext _dbContext;

    public InvitacionEquipoRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct)
    {
        try
        {
            await _dbContext.InvitacionesEquipo.AddAsync(invitacion, ct);
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("No fue posible persistir la invitación.", ex);
        }
    }

    public async Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct)
    {
        try
        {
            var entry = _dbContext.Entry(invitacion);

            if (entry.State == EntityState.Detached)
            {
                _dbContext.InvitacionesEquipo.Attach(invitacion);
                entry.State = EntityState.Modified;
            }

            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new PersistenceException("No fue posible actualizar la invitación (concurrencia).", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("No fue posible actualizar la invitación.", ex);
        }
    }

    public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
    {
        return _dbContext.InvitacionesEquipo
            .FirstOrDefaultAsync(x => x.InvitacionEquipoId == invitacionId, ct);
    }

    public async Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
    {
        var list = await _dbContext.InvitacionesEquipo
            .Where(x => x.InvitadoUserId == invitadoUserId && x.Estado == EstadoInvitacion.Pendiente)
            .ToListAsync(ct);

        return list;
    }

    public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
    {
        return _dbContext.InvitacionesEquipo
            .AnyAsync(
                x => x.EquipoId == equipoId &&
                     x.InvitadoUserId == invitadoUserId &&
                     x.Estado == EstadoInvitacion.Pendiente,
                ct);
    }

    public async Task<IReadOnlyCollection<Guid>> GetInvitadoUserIdsPendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
    {
        return await _dbContext.InvitacionesEquipo
            .Where(x => x.EquipoId == equipoId && x.Estado == EstadoInvitacion.Pendiente)
            .Select(x => x.InvitadoUserId)
            .ToListAsync(ct);
    }

    public async Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
    {
        try
        {
            var pendientes = await _dbContext.InvitacionesEquipo
                .Where(x => x.EquipoId == equipoId && x.Estado == EstadoInvitacion.Pendiente)
                .ToListAsync(ct);

            if (pendientes.Count > 0)
            {
                _dbContext.InvitacionesEquipo.RemoveRange(pendientes);
                await _dbContext.SaveChangesAsync(ct);
            }
        }
        catch (DbUpdateException ex)
        {
            throw new PersistenceException("No fue posible eliminar las invitaciones pendientes del equipo.", ex);
        }
    }
}
