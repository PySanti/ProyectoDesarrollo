export type Participante = { usuarioId: string; nombre: string; esLider: boolean };

// `fetchMyTeamStatus` (teamPanelFlow.js) has no JSDoc, so TS infers/widens its return type from the
// plain object literals instead of preserving the `ok` discriminant. Annotate the real shape here.
// Shared by TeamPanelScreen.tsx and TransferLeadershipScreen.tsx — both consume this function.
export type FetchTeamStatusResult =
  | { ok: false; type?: string; message?: string }
  | { ok: true; status: "sinEquipo" }
  | {
      ok: true;
      status: "lider" | "miembro";
      equipoId: string;
      nombreEquipo: string;
      participantes: Participante[];
    };
