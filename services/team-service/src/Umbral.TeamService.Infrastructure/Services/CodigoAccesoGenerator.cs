using Umbral.TeamService.Application.Abstractions.Persistence;
using Umbral.TeamService.Application.Abstractions.Services;
using Umbral.TeamService.Application.Exceptions;

namespace Umbral.TeamService.Infrastructure.Services;

public sealed class CodigoAccesoGenerator : ICodigoAccesoGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 8;
    private const int MaxAttempts = 20;

    private readonly IEquipoRepository _equipoRepository;

    public CodigoAccesoGenerator(IEquipoRepository equipoRepository)
    {
        _equipoRepository = equipoRepository;
    }

    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = GenerateCandidate();
            var exists = await _equipoRepository.ExistsByAccessCodeAsync(code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        throw new AccessCodeGenerationException("No fue posible generar un codigo de acceso unico.");
    }

    private static string GenerateCandidate()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(chars);
    }
}
