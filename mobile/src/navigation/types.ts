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
  TriviaGamesList: undefined;
  TriviaLobby: { partidaId: string };
  TriviaLivePlay: { partidaId: string };
  TriviaAnswer: { partidaId: string };
  TriviaResult: { partidaId: string; preguntaId: string };
  TriviaScore: { partidaId: string };
  BdtPublishedGames: undefined;
  BdtRanking: undefined;
  BdtActiveStage: { partidaId: string };
  BdtTreasureUpload: { partidaId: string; etapaId: string };
};
