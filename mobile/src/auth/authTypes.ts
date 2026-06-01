export type AuthUser = {
  sub: string;
  username: string;
  roles: string[];
};

export type AuthSessionState = {
  token: string;
  user: AuthUser;
};
