export type AuthStackParamList = {
  Login: undefined;
};

export type AppStackParamList = {
  Home: undefined;
  CreateTeam: undefined;
  Invitations: undefined;
  InviteMember: undefined;
  TransferLeadership: undefined;
  LeaveTeam: undefined;
  PartidasPanel: undefined;
  PartidaLobby: { partidaId: string; nombre: string };
  PartidaLive: { partidaId: string; nombre: string };
  Convocatorias: undefined;
};
