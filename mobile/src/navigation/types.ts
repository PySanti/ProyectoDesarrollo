export type AuthStackParamList = {
  Login: undefined;
};

export type AppStackParamList = {
  Home: undefined;
  CreateTeam: undefined;
  JoinTeam: undefined;
  TransferLeadership: undefined;
  LeaveTeam: undefined;
  TriviaGamesList: undefined;
  TriviaLobby: { partidaId: string };
  TriviaAnswer: { partidaId: string };
  TriviaResult: { partidaId: string; preguntaId: string };
  TriviaScore: { partidaId: string };
  BdtPublishedGames: undefined;
  BdtActiveStage: { partidaId: string };
  BdtTreasureUpload: { partidaId: string; etapaId: string };
};
