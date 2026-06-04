export type AuthStackParamList = {
  Login: undefined;
};

export type AppStackParamList = {
  Home: undefined;
  CreateTeam: undefined;
  JoinTeam: undefined;
  TransferLeadership: undefined;
  LeaveTeam: undefined;
  BdtPublishedGames: undefined;
  BdtActiveStage: { partidaId: string };
  BdtTreasureUpload: { partidaId: string; etapaId: string };
};
