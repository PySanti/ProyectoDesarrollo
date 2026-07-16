using Microsoft.EntityFrameworkCore;

namespace Umbral.IdentityService.Infrastructure.Persistence;

/// <summary>
/// EnsureCreated no evoluciona esquemas existentes: si la BD ya tiene alguna tabla, no crea las
/// que falten. Estos parches cubren la deriva y deben correr ANTES del backfill de historial, que
/// consulta historial_nombre_equipo.
/// </summary>
public static class EsquemaLegadoPatch
{
    public static async Task AplicarAsync(IdentityDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS usuarios (
                usuarioid uuid PRIMARY KEY,
                keycloakid varchar(128) NOT NULL,
                nombre varchar(120) NOT NULL,
                correo varchar(320) NOT NULL,
                rol integer NOT NULL,
                estado integer NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_usuarios_correo ON usuarios (correo);
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS permisos_rol (
                rol integer NOT NULL,
                permiso integer NOT NULL,
                PRIMARY KEY (rol, permiso)
            );

            CREATE TABLE IF NOT EXISTS migraciones_aplicadas (
                nombre varchar(200) PRIMARY KEY,
                fechaaplicacionutc timestamp with time zone NOT NULL
            );

            -- Reset a los defaults del modelo de dos privilegios: Administrador->GestionarEquipos,
            -- Operador->GestionarPartidas, Participante->ninguno. El bloque es atomico y corre UNA
            -- sola vez: sin el guardia, cada arranque borraria lo asignado desde el panel.
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM migraciones_aplicadas WHERE nombre = '2026-07-15-dos-privilegios') THEN
                    DELETE FROM permisos_rol;
                    INSERT INTO permisos_rol (rol, permiso) VALUES (1, 2), (2, 1);
                    INSERT INTO migraciones_aplicadas (nombre, fechaaplicacionutc)
                    VALUES ('2026-07-15-dos-privilegios', now());
                END IF;
            END $$;
            """, cancellationToken);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS equipos (
                equipoid uuid PRIMARY KEY,
                nombreequipo varchar(120) NOT NULL,
                estado integer NOT NULL
            );

            CREATE TABLE IF NOT EXISTS equipos_participantes (
                participanteequipoid uuid PRIMARY KEY,
                equipoid uuid NOT NULL REFERENCES equipos (equipoid) ON DELETE CASCADE,
                usuarioid uuid NOT NULL,
                fechaunionutc timestamp with time zone NOT NULL,
                eslider boolean NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_equipos_participantes_usuarioid ON equipos_participantes (usuarioid);

            -- Deriva de cuando equipos_participantes.equipoid era opcional: EF nuleaba la FK al
            -- salir del equipo en vez de borrar la fila, y la huerfana seguia ocupando el slot del
            -- usuario en ux_equipos_participantes_usuarioid ("ya perteneces a un equipo activo" al
            -- crear otro). Ambas sentencias son idempotentes y no necesitan guardia: con la columna
            -- NOT NULL una huerfana nueva ya no puede existir. El DELETE va primero o el ALTER falla.
            DELETE FROM equipos_participantes WHERE equipoid IS NULL;
            ALTER TABLE equipos_participantes ALTER COLUMN equipoid SET NOT NULL;

            CREATE TABLE IF NOT EXISTS invitaciones_equipo (
                invitacionequipoid uuid PRIMARY KEY,
                equipoid uuid NOT NULL REFERENCES equipos (equipoid) ON DELETE CASCADE,
                invitadouserid uuid NOT NULL,
                invitadoporuserid uuid NOT NULL,
                estado integer NOT NULL,
                fechacreacionutc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_invitaciones_equipo_invitadouserid ON invitaciones_equipo (invitadouserid);

            CREATE TABLE IF NOT EXISTS historial_nombre_equipo (
                id uuid PRIMARY KEY,
                usuarioid uuid NOT NULL,
                equipoid uuid NOT NULL,
                nombreequipo varchar(120) NOT NULL,
                fecharegistroutc timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_historial_nombre_equipo_usuarioid ON historial_nombre_equipo (usuarioid);

            CREATE TABLE IF NOT EXISTS participaciones_activas_equipo (
                equipoid uuid NOT NULL,
                partidaid uuid NOT NULL,
                fecharegistroutc timestamp with time zone NOT NULL,
                PRIMARY KEY (equipoid, partidaid)
            );
            """, cancellationToken);
    }
}
